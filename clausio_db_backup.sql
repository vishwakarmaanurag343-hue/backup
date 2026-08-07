--
-- PostgreSQL database dump
--

\restrict rozdD6grJrezTlkhX1Sy55xWjfa9aEy9u3yOXk9dz54qTSkg2dIT8Bn2a852Hh3

-- Dumped from database version 16.14
-- Dumped by pg_dump version 16.14

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: ActionPlans; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ActionPlans" (
    "Id" uuid NOT NULL,
    "Title" text,
    "Description" text,
    "Priority" text,
    "DueBy" timestamp with time zone NOT NULL,
    "AssignedTo" text,
    "Done" boolean NOT NULL,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."ActionPlans" OWNER TO postgres;

--
-- Name: Cases; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Cases" (
    "Id" uuid NOT NULL,
    "Name" text,
    "CaseNumber" text,
    "CaseType" text,
    "SubType" text,
    "Court" text,
    "CourtLocation" text,
    "Stage" text,
    "Status" text,
    "Priority" text,
    "OpposingAdv" text,
    "FiledOn" timestamp with time zone NOT NULL,
    "NextHearing" timestamp with time zone,
    "ReadinessScore" integer,
    "ClientId" uuid NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Cases" OWNER TO postgres;

--
-- Name: Clients; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Clients" (
    "Id" uuid NOT NULL,
    "FirstName" text,
    "LastName" text,
    "Phone" text,
    "AltPhone" text,
    "Email" text,
    "WhatsApp" text,
    "Address" text,
    "ClientType" text,
    "Aadhar" text,
    "Pan" text,
    "Occupation" text,
    "MonthlyIncome" numeric(18,2),
    "BankName" text,
    "IsVip" boolean NOT NULL,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Clients" OWNER TO postgres;

--
-- Name: Contradictions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Contradictions" (
    "Id" uuid NOT NULL,
    "Claim" text,
    "ClaimSource" text,
    "Evidence" text,
    "EvidenceSource" text,
    "CourtArgument" text,
    "Strength" text,
    "Used" boolean NOT NULL,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Contradictions" OWNER TO postgres;

--
-- Name: Documents; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Documents" (
    "Id" uuid NOT NULL,
    "FileName" character varying(500) NOT NULL,
    "DocumentType" text,
    "ExhibitLabel" text,
    "StoragePath" character varying(1000) NOT NULL,
    "ContentType" text,
    "SizeBytes" bigint NOT NULL,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Documents" OWNER TO postgres;

--
-- Name: HearingOrders; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."HearingOrders" (
    "Id" uuid NOT NULL,
    "Text" text,
    "Responsible" text,
    "Deadline" timestamp with time zone NOT NULL,
    "Done" boolean NOT NULL,
    "HearingId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."HearingOrders" OWNER TO postgres;

--
-- Name: Hearings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Hearings" (
    "Id" uuid NOT NULL,
    "HearingDate" timestamp with time zone NOT NULL,
    "Stage" text,
    "Judge" text,
    "CourtHall" text,
    "WhatHappened" text,
    "JudgeObservation" text,
    "OpposingAdmission" text,
    "NextObjective" text,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Hearings" OWNER TO postgres;

--
-- Name: LegalResearches; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."LegalResearches" (
    "Id" uuid NOT NULL,
    "Citation" text,
    "Court" text,
    "Year" integer NOT NULL,
    "RatioDecidendi" text,
    "Relevance" text,
    "HowToUse" text,
    "Strength" text,
    "FullJudgmentUrl" text,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."LegalResearches" OWNER TO postgres;

--
-- Name: ReadinessChecklistItems; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ReadinessChecklistItems" (
    "Id" uuid NOT NULL,
    "Text" text,
    "Category" text,
    "Done" boolean NOT NULL,
    "ReadinessId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."ReadinessChecklistItems" OWNER TO postgres;

--
-- Name: Readinesses; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Readinesses" (
    "Id" uuid NOT NULL,
    "Score" integer NOT NULL,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Readinesses" OWNER TO postgres;

--
-- Name: TimelineEvents; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."TimelineEvents" (
    "Id" uuid NOT NULL,
    "EventDate" timestamp with time zone NOT NULL,
    "Event" text,
    "Source" text,
    "LegalSignificance" text,
    "Category" text,
    "SortOrder" integer NOT NULL,
    "CaseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."TimelineEvents" OWNER TO postgres;

--
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "FirstName" character varying(200) NOT NULL,
    "LastName" character varying(200) NOT NULL,
    "Email" character varying(320) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" text,
    "Phone" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- Data for Name: ActionPlans; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ActionPlans" ("Id", "Title", "Description", "Priority", "DueBy", "AssignedTo", "Done", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Cases; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Cases" ("Id", "Name", "CaseNumber", "CaseType", "SubType", "Court", "CourtLocation", "Stage", "Status", "Priority", "OpposingAdv", "FiledOn", "NextHearing", "ReadinessScore", "ClientId", "CreatedByUserId", "CreatedAt", "UpdatedAt") FROM stdin;
f7b35758-69b1-4340-bc37-03868eb99e55	Priya v. Rohit Sharma	FC/2847/2024	Family	Divorce Petition	Family Court	Bandra Mumbai	Evidence	Active	High	\N	2024-01-15 05:30:00+05:30	\N	\N	ed8213bd-09a9-4298-a00f-23b7e59c258e	bd5b1f2b-b9a6-4423-b4fa-822f0f7849c3	2026-07-22 18:11:12.791525+05:30	2026-07-22 18:11:12.791525+05:30
\.


--
-- Data for Name: Clients; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Clients" ("Id", "FirstName", "LastName", "Phone", "AltPhone", "Email", "WhatsApp", "Address", "ClientType", "Aadhar", "Pan", "Occupation", "MonthlyIncome", "BankName", "IsVip", "Notes", "CreatedAt", "UpdatedAt") FROM stdin;
ed8213bd-09a9-4298-a00f-23b7e59c258e	Priya	Sharma	+91 98765 43210	\N	priya@gmail.com	\N	Mumbai	\N	\N	\N	\N	\N	\N	f	\N	2026-07-22 18:10:43.689966+05:30	2026-07-22 18:10:43.689966+05:30
\.


--
-- Data for Name: Contradictions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Contradictions" ("Id", "Claim", "ClaimSource", "Evidence", "EvidenceSource", "CourtArgument", "Strength", "Used", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Documents; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Documents" ("Id", "FileName", "DocumentType", "ExhibitLabel", "StoragePath", "ContentType", "SizeBytes", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: HearingOrders; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."HearingOrders" ("Id", "Text", "Responsible", "Deadline", "Done", "HearingId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Hearings; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Hearings" ("Id", "HearingDate", "Stage", "Judge", "CourtHall", "WhatHappened", "JudgeObservation", "OpposingAdmission", "NextObjective", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: LegalResearches; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."LegalResearches" ("Id", "Citation", "Court", "Year", "RatioDecidendi", "Relevance", "HowToUse", "Strength", "FullJudgmentUrl", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: ReadinessChecklistItems; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ReadinessChecklistItems" ("Id", "Text", "Category", "Done", "ReadinessId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Readinesses; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Readinesses" ("Id", "Score", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: TimelineEvents; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."TimelineEvents" ("Id", "EventDate", "Event", "Source", "LegalSignificance", "Category", "SortOrder", "CaseId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "Role", "Phone", "CreatedAt", "UpdatedAt") FROM stdin;
3051feed-c795-48e4-a9f8-742772753be7	Parth	Bindra	parth@gmail.com	AQAAAAIAAYagAAAAEMQoBWctJKPVTkLj4i4/99r3bWn5qMOnwSUZlMM1YS69sQbtE0bEsi5VkJQKiYuQfQ==	CEO	142	2026-07-22 00:13:08.719682+05:30	2026-07-22 00:13:08.719682+05:30
3c7792ec-70b1-4fb5-9ea6-22d41084dad7	omkar	Morevkar	o@gmail.com	AQAAAAIAAYagAAAAEGmRwGlt6nQOqV41171VgTpS1s6kOdtcUIZsD5eMrpiISWuBd5MC0wocx+DlBX/ReQ==	CTO	123	2026-07-22 00:35:40.81292+05:30	2026-07-22 00:35:40.81292+05:30
2d6840a8-ffdc-4f2b-abf5-e17718cebc43	Parth	Bindra	parth@clausio.io	AQAAAAIAAYagAAAAEJbd2u2Ac+kL2XL1pOse1Q33baQN1OAo1Fi2/HByPuZDIJLIVhUokCJbz9Rng/3zGA==	SeniorAdvocate	+91 98765 43210	2026-07-22 18:00:20.418707+05:30	2026-07-22 18:00:20.418707+05:30
bd5b1f2b-b9a6-4423-b4fa-822f0f7849c3	Parth	Bindra	test@clausio.io	AQAAAAIAAYagAAAAEPDZTAly3uvreKIBoaUN05fzoPq4J67mCG682vQgfTNFSHme/7kJaEZFWrpkNOaytA==	SeniorAdvocate	\N	2026-07-22 18:07:49.282221+05:30	2026-07-22 18:07:49.282221+05:30
\.


--
-- Name: ActionPlans PK_ActionPlans; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ActionPlans"
    ADD CONSTRAINT "PK_ActionPlans" PRIMARY KEY ("Id");


--
-- Name: Cases PK_Cases; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cases"
    ADD CONSTRAINT "PK_Cases" PRIMARY KEY ("Id");


--
-- Name: Clients PK_Clients; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Clients"
    ADD CONSTRAINT "PK_Clients" PRIMARY KEY ("Id");


--
-- Name: Contradictions PK_Contradictions; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Contradictions"
    ADD CONSTRAINT "PK_Contradictions" PRIMARY KEY ("Id");


--
-- Name: Documents PK_Documents; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Documents"
    ADD CONSTRAINT "PK_Documents" PRIMARY KEY ("Id");


--
-- Name: HearingOrders PK_HearingOrders; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."HearingOrders"
    ADD CONSTRAINT "PK_HearingOrders" PRIMARY KEY ("Id");


--
-- Name: Hearings PK_Hearings; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Hearings"
    ADD CONSTRAINT "PK_Hearings" PRIMARY KEY ("Id");


--
-- Name: LegalResearches PK_LegalResearches; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."LegalResearches"
    ADD CONSTRAINT "PK_LegalResearches" PRIMARY KEY ("Id");


--
-- Name: ReadinessChecklistItems PK_ReadinessChecklistItems; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ReadinessChecklistItems"
    ADD CONSTRAINT "PK_ReadinessChecklistItems" PRIMARY KEY ("Id");


--
-- Name: Readinesses PK_Readinesses; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Readinesses"
    ADD CONSTRAINT "PK_Readinesses" PRIMARY KEY ("Id");


--
-- Name: TimelineEvents PK_TimelineEvents; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TimelineEvents"
    ADD CONSTRAINT "PK_TimelineEvents" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: IX_ActionPlans_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ActionPlans_CaseId" ON public."ActionPlans" USING btree ("CaseId");


--
-- Name: IX_Cases_CaseNumber; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cases_CaseNumber" ON public."Cases" USING btree ("CaseNumber");


--
-- Name: IX_Cases_ClientId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cases_ClientId" ON public."Cases" USING btree ("ClientId");


--
-- Name: IX_Cases_CreatedByUserId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Cases_CreatedByUserId" ON public."Cases" USING btree ("CreatedByUserId");


--
-- Name: IX_Contradictions_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Contradictions_CaseId" ON public."Contradictions" USING btree ("CaseId");


--
-- Name: IX_Documents_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Documents_CaseId" ON public."Documents" USING btree ("CaseId");


--
-- Name: IX_HearingOrders_HearingId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_HearingOrders_HearingId" ON public."HearingOrders" USING btree ("HearingId");


--
-- Name: IX_Hearings_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_Hearings_CaseId" ON public."Hearings" USING btree ("CaseId");


--
-- Name: IX_LegalResearches_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_LegalResearches_CaseId" ON public."LegalResearches" USING btree ("CaseId");


--
-- Name: IX_ReadinessChecklistItems_ReadinessId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_ReadinessChecklistItems_ReadinessId" ON public."ReadinessChecklistItems" USING btree ("ReadinessId");


--
-- Name: IX_Readinesses_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Readinesses_CaseId" ON public."Readinesses" USING btree ("CaseId");


--
-- Name: IX_TimelineEvents_CaseId; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX "IX_TimelineEvents_CaseId" ON public."TimelineEvents" USING btree ("CaseId");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: ActionPlans FK_ActionPlans_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ActionPlans"
    ADD CONSTRAINT "FK_ActionPlans_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: Cases FK_Cases_Clients_ClientId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cases"
    ADD CONSTRAINT "FK_Cases_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES public."Clients"("Id") ON DELETE RESTRICT;


--
-- Name: Cases FK_Cases_Users_CreatedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Cases"
    ADD CONSTRAINT "FK_Cases_Users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: Contradictions FK_Contradictions_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Contradictions"
    ADD CONSTRAINT "FK_Contradictions_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: Documents FK_Documents_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Documents"
    ADD CONSTRAINT "FK_Documents_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: HearingOrders FK_HearingOrders_Hearings_HearingId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."HearingOrders"
    ADD CONSTRAINT "FK_HearingOrders_Hearings_HearingId" FOREIGN KEY ("HearingId") REFERENCES public."Hearings"("Id") ON DELETE CASCADE;


--
-- Name: Hearings FK_Hearings_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Hearings"
    ADD CONSTRAINT "FK_Hearings_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: LegalResearches FK_LegalResearches_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."LegalResearches"
    ADD CONSTRAINT "FK_LegalResearches_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: ReadinessChecklistItems FK_ReadinessChecklistItems_Readinesses_ReadinessId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ReadinessChecklistItems"
    ADD CONSTRAINT "FK_ReadinessChecklistItems_Readinesses_ReadinessId" FOREIGN KEY ("ReadinessId") REFERENCES public."Readinesses"("Id") ON DELETE CASCADE;


--
-- Name: Readinesses FK_Readinesses_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Readinesses"
    ADD CONSTRAINT "FK_Readinesses_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- Name: TimelineEvents FK_TimelineEvents_Cases_CaseId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."TimelineEvents"
    ADD CONSTRAINT "FK_TimelineEvents_Cases_CaseId" FOREIGN KEY ("CaseId") REFERENCES public."Cases"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict rozdD6grJrezTlkhX1Sy55xWjfa9aEy9u3yOXk9dz54qTSkg2dIT8Bn2a852Hh3

