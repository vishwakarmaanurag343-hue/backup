import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// Controls the 3 collapsible panels
// persist = saved to localStorage, survives refresh
interface UIState {
  sidebarExpanded: boolean
  caseListVisible: boolean
  aiPanelVisible:  boolean
  aiPanelExpanded: boolean
  aiPanelWidth:    number
  language:        string
  toggleSidebar:   () => void
  toggleCaseList:  () => void
  toggleAIPanel:   () => void
  toggleAIPanelExpand: () => void
  setAIPanelWidth: (width: number) => void
  setLanguage:     (lang: string) => void
}

export const useUIStore = create<UIState>()(
  persist(
    (set) => ({
      sidebarExpanded: false,
      caseListVisible: true,
      aiPanelVisible:  true,
      aiPanelExpanded: false,
      aiPanelWidth:    320,
      language:        'en',
      toggleSidebar:   () => set(s => ({ sidebarExpanded: !s.sidebarExpanded })),
      toggleCaseList:  () => set(s => ({ caseListVisible: !s.caseListVisible  })),
      toggleAIPanel:   () => set(s => ({ aiPanelVisible:  !s.aiPanelVisible   })),
      toggleAIPanelExpand: () => set(s => ({ aiPanelExpanded: !s.aiPanelExpanded, aiPanelWidth: s.aiPanelExpanded ? 320 : 520 })),
      setAIPanelWidth: (w) => set({ aiPanelWidth: Math.max(220, Math.min(800, w)) }),
      setLanguage:     (lang) => set({ language: lang }),
    }),
    { name: 'clausio-ui' }
  )
)

// Stores who is logged in
interface AuthState {
  userName:  string
  userRole:  string
  isLoggedIn: boolean
  login:  (name: string, role: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      userName:   'Parth Bindra',
      userRole:   'Senior Adv.',
      isLoggedIn: false,
      login:  (name, role) => set({ userName: name, userRole: role, isLoggedIn: true }),
      logout: () => set({ isLoggedIn: false }),
    }),
    { name: 'clausio-auth' }
  )
)

// ✅ NEW: Stores which case is currently selected
// When lawyer clicks a case in CaseList — this updates
// Dashboard reads this to show correct case data
interface CaseState {
  selectedCaseId:   string
  selectedCaseName: string
  setSelectedCase:  (id: string, name: string) => void
}

export const useCaseStore = create<CaseState>()(
  persist(
    (set) => ({
      selectedCaseId:   '',
      selectedCaseName: '',
      setSelectedCase:  (id, name) => set({ selectedCaseId: id, selectedCaseName: name }),
    }),
    { name: 'clausio-case' }
  )
)
