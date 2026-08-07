import asyncio
import json
import numpy as np
import os
import site
import traceback
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from faster_whisper import WhisperModel
import torch

try:
    from paddleocr import PaddleOCR
    import pypdfium2 as pdfium
except ImportError:
    PaddleOCR = None
    pdfium = None

# Workaround for Python 3.13 Windows DLL loading (WinError 126)
user_site = site.getusersitepackages()
torch_lib_path = os.path.join(user_site, "torch", "lib")
if os.path.exists(torch_lib_path):
    os.environ["PATH"] = torch_lib_path + os.pathsep + os.environ.get("PATH", "")
    try:
        os.add_dll_directory(torch_lib_path)
    except AttributeError:
        pass

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Configuration
MODEL_SIZE = "base.en"
SAMPLE_RATE = 16000

# Device configuration
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device}")

# Lazy loading models
whisper_model = None
paddle_ocr = None

def load_models():
    global whisper_model, paddle_ocr
    try:
        if whisper_model is None:
            print(f"Loading faster-whisper model ({MODEL_SIZE})...")
            # compute_type="float16" optimizes memory and speed on GPU
            compute_type = "float16" if device == "cuda" else "int8"
            whisper_model = WhisperModel(MODEL_SIZE, device=device, compute_type=compute_type)
            print("faster-whisper loaded successfully.")
    except Exception as e:
        print(f"Error loading whisper model: {e}")
        
    try:
        if paddle_ocr is None and PaddleOCR is not None:
            print("Loading PaddleOCR model...")
            paddle_ocr = PaddleOCR(lang='en')
            print("PaddleOCR loaded successfully.")
    except Exception as e:
        print(f"Error loading PaddleOCR model: {e}")

@app.on_event("startup")
async def startup_event():
    loop = asyncio.get_event_loop()
    await loop.run_in_executor(None, load_models)

@app.post("/api/voice/raw")
async def transcribe_voice_raw(request: Request):
    """Accept raw float32 PCM bytes at 16kHz mono and return transcribed text."""
    import warnings
    body = await request.body()
    if len(body) < 1000:
        return {"text": ""}
    
    audio_np = np.frombuffer(body, dtype=np.float32).copy()
    
    # --- Sanitize: replace NaN/Inf with 0 to prevent CTranslate2 crashes ---
    audio_np = np.nan_to_num(audio_np, nan=0.0, posinf=0.0, neginf=0.0)
    
    max_amp = float(np.max(np.abs(audio_np)))
    print(f"[voice/raw] samples={len(audio_np)}, max_amp={max_amp:.4f}")
    
    if max_amp < 0.0001:
        print("[voice/raw] Audio too quiet, skipping")
        return {"text": ""}
    
    # Normalize to [-0.9, 0.9]
    audio_np = (audio_np / max_amp * 0.9).astype(np.float32)
    
    if not whisper_model:
        raise HTTPException(status_code=503, detail="Whisper model not loaded yet")
    
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")  # suppress mel spectrogram RuntimeWarnings
            segments, _ = whisper_model.transcribe(
                audio_np,
                beam_size=5,
                language="en",
                vad_filter=True,
                vad_parameters=dict(min_silence_duration_ms=300),
                temperature=0.0
            )
        text = " ".join(s.text.strip() for s in segments).strip()
        # Filter Whisper hallucinations on silence
        hallucinations = {"you.", "thank you.", "thank you", "bye.", "you"}
        if text.strip().lower() in hallucinations:
            text = ""
        print(f"[voice/raw] Transcribed: '{text}'")
        return {"text": text}
    except Exception as e:
        print(f"[voice/raw] Whisper error (transient): {e}")
        return {"text": ""}  # Return empty instead of crashing

@app.post("/api/voice")
async def transcribe_voice(file: UploadFile = File(...)):
    """Accept a webm/ogg audio blob from the browser and return transcribed text."""
    import tempfile, subprocess
    
    # Write incoming audio to a temp file
    suffix = ".webm"
    with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
        tmp.write(await file.read())
        tmp_path = tmp.name

    wav_path = tmp_path.replace(".webm", ".wav")
    
    try:
        # Convert webm → wav 16kHz mono using ffmpeg
        result = subprocess.run(
            ["ffmpeg", "-y", "-i", tmp_path, "-ar", "16000", "-ac", "1", "-f", "wav", wav_path],
            capture_output=True, timeout=15
        )
        
        if result.returncode != 0:
            raise HTTPException(status_code=500, detail=f"ffmpeg failed: {result.stderr.decode()}")
        
        if not whisper_model:
            raise HTTPException(status_code=503, detail="Whisper model not loaded yet")
        
        import os as _os
        import wave, struct
        import warnings
        wav_size = _os.path.getsize(wav_path) if _os.path.exists(wav_path) else 0
        print(f"[voice] WAV size: {wav_size} bytes, transcribing...")
        
        # Load WAV as float32 numpy array and normalize amplitude
        with wave.open(wav_path, 'rb') as wf:
            frames = wf.readframes(wf.getnframes())
            audio_np = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
            
        # Sanitize NaN/Inf
        audio_np = np.nan_to_num(audio_np, nan=0.0, posinf=0.0, neginf=0.0)
        
        max_amp = float(np.max(np.abs(audio_np))) if len(audio_np) > 0 else 0
        print(f"[voice] Max amplitude: {max_amp:.4f}")
        
        # Normalize to prevent near-zero mel spectrogram issues
        if max_amp > 0.0001:
            audio_np = (audio_np / max_amp * 0.9).astype(np.float32)
        else:
            print("[voice] Audio too quiet, returning empty")
            return {"text": ""}
        
        try:
            with warnings.catch_warnings():
                warnings.simplefilter("ignore")
                segments, _ = whisper_model.transcribe(
                    audio_np,
                    beam_size=5,
                    language="en",
                    vad_filter=True,
                    vad_parameters=dict(min_silence_duration_ms=300),
                    temperature=0.0
                )
            text = " ".join(s.text.strip() for s in segments).strip()
            
            hallucinations = {"you.", "thank you.", "thank you", "bye.", "you"}
            if text.strip().lower() in hallucinations:
                text = ""
                
            print(f"[voice] Transcribed: '{text}'")
            return {"text": text}
        except Exception as e:
            print(f"[voice] Whisper error: {e}")
            return {"text": ""}
    
    except subprocess.TimeoutExpired:
        raise HTTPException(status_code=504, detail="Transcription timed out")
    finally:
        for p in [tmp_path, wav_path]:
            if os.path.exists(p):
                os.remove(p)

@app.post("/api/ocr")
async def process_ocr(file: UploadFile = File(...)):
    # Save the file temporarily
    temp_dir = "temp_uploads"
    os.makedirs(temp_dir, exist_ok=True)
    temp_path = os.path.join(temp_dir, file.filename)
    
    with open(temp_path, "wb") as f:
        f.write(await file.read())
        
    try:
        # Fallback if PaddleOCR failed to load (e.g. on Python 3.13)
        if paddle_ocr is None:
            print(f"Fallback mode: Mocking OCR for {file.filename} because PaddleOCR is missing.")
            await asyncio.sleep(2) # Simulate processing time
            return {
                "text": f"--- MOCK OCR RESULT ---\n\nExtracted text from {file.filename}.\n\n(Note: PaddleOCR is not installed due to Python version compatibility, so this is simulated text.)", 
                "filename": file.filename
            }
            
        # Process Image or PDF
        results = list(paddle_ocr.predict(temp_path))
        full_text = []
        if results:
            for page_result in results:
                if isinstance(page_result, dict) and "rec_texts" in page_result:
                    full_text.extend(page_result["rec_texts"])
                elif isinstance(page_result, list):
                    # Fallback for old PaddleOCR format just in case
                    for line in page_result:
                        if len(line) > 1 and len(line[1]) > 0:
                            full_text.append(line[1][0])
        extracted_text = "\n".join(full_text)
        
        return {"text": extracted_text, "filename": file.filename}
    except Exception as e:
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        # Cleanup
        if os.path.exists(temp_path):
            os.remove(temp_path)

@app.websocket("/ws/transcribe")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    
    language = "auto" # default to auto-detect
    audio_buffer = []
    last_processed_len = 0
    
    # Common hallucinations Whisper outputs when it hears silence or noise
    hallucinations = ["you", "you.", "thank you.", "thank you", "bye.", "嗨", "what's your name?", "what's your name", "wait, wait, wait."]
    
    try:
        while True:
            message = await websocket.receive()
            
            if "text" in message:
                try:
                    data = json.loads(message["text"])
                    if "language" in data:
                        language = data["language"]
                        print(f"Language preference received: {language}")
                except json.JSONDecodeError:
                    pass
            elif "bytes" in message:
                pcm_data = np.frombuffer(message["bytes"], dtype=np.float32)
                audio_buffer.extend(pcm_data.tolist())
                
                # Debug log every ~0.5 seconds
                if len(audio_buffer) % (SAMPLE_RATE // 2) < len(pcm_data):
                    print(f"Audio buffer size: {len(audio_buffer)}. Max amp: {np.max(np.abs(pcm_data))}")
                
                # Process every 0.5 seconds of new audio for live streaming feel
                if len(audio_buffer) - last_processed_len >= SAMPLE_RATE * 0.5:
                    last_processed_len = len(audio_buffer)
                    audio_array = np.array(audio_buffer, dtype=np.float32)
                    
                    try:
                        if whisper_model:
                            segments, info = whisper_model.transcribe(
                                audio_array, 
                                beam_size=1, # fast beam search for live
                                language=None if language == "auto" else language,
                                vad_filter=True, 
                                vad_parameters=dict(min_silence_duration_ms=500),
                                condition_on_previous_text=False,
                                temperature=0.0 # greedy decoding for speed
                            )
                            
                            text = "".join([segment.text for segment in segments]).strip()
                            text_lower = text.lower()
                            
                            if text_lower and text_lower not in hallucinations and "zajed" not in text_lower and "продолжение следует" not in text_lower:
                                await websocket.send_text(json.dumps({
                                    "text": text,
                                    "isFinal": False 
                                }))
                                
                    except Exception as e:
                        print(f"Transcription error: {e}")
                        traceback.print_exc()
                        audio_buffer = []

    except WebSocketDisconnect:
        print("Client disconnected")
    except RuntimeError as e:
        if "disconnect message has been received" in str(e):
            print("Client disconnected (RuntimeError)")
        else:
            print(f"RuntimeError: {e}")
    except Exception as e:
        print(f"Unexpected error: {e}")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("app:app", host="0.0.0.0", port=8000, reload=True)
