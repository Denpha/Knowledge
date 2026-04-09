# RAG Benchmark Workflow

This document describes a repeatable benchmark loop for the endpoints:
- POST /api/ai/evaluate-batch
- POST /api/ai/evaluate-compare

## 1) Prepare benchmark cases
Create a JSON file (example: rag-benchmark-cases.json):

```json
{
  "promptProfile": "balanced",
  "topK": 5,
  "maxContextChars": 5000,
  "semanticThreshold": 0.65,
  "cases": [
    {
      "caseId": "admission-001",
      "question": "ขั้นตอนยื่นขอเอกสารรับรองนักศึกษามีอะไรบ้าง",
      "expectedKeywords": ["เอกสาร", "คำร้อง", "ระยะเวลา"]
    },
    {
      "caseId": "it-helpdesk-001",
      "question": "หากลืมรหัสผ่านระบบมหาวิทยาลัยต้องทำอย่างไร",
      "expectedKeywords": ["รีเซ็ตรหัสผ่าน", "ยืนยันตัวตน", "เจ้าหน้าที่"]
    }
  ]
}
```

## 2) Run benchmark
Use a valid JWT token with AI access:

```bash
curl -X POST "http://127.0.0.1:5000/api/ai/evaluate-batch" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json" \
  --data @rag-benchmark-cases.json
```

Supported prompt profiles:
- default
- balanced
- strict

## 2.1) Compare prompt profiles (A/B/C)

```bash
curl -X POST "http://127.0.0.1:5000/api/ai/evaluate-compare" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "profiles": ["default", "balanced", "strict"],
    "topK": 5,
    "maxContextChars": 5000,
    "semanticThreshold": 0.65,
    "cases": [
      {
        "caseId": "admission-001",
        "question": "ขั้นตอนยื่นขอเอกสารรับรองนักศึกษามีอะไรบ้าง",
        "expectedKeywords": ["เอกสาร", "คำร้อง", "ระยะเวลา"]
      }
    ]
  }'
```

Response returns profile summaries sorted by highest passRate.

## 3) Read summary metrics
Response includes:
- totalCases
- passedCases
- passRate
- averageAnswerCoverage
- averageContextCoverage
- caseResults[]

Pass condition per case:
- answerCoverage >= 0.60
- contextCoverage >= 0.70

## 4) Improvement loop
1. Run benchmark on baseline prompt strategy.
2. Tune retrieval/prompt settings (topK, threshold, prompt profile).
3. Re-run benchmark and compare passRate + average coverage across profiles.
4. Keep changes only when passRate improves and no major regression appears in critical cases.

## 5) Suggested operational cadence
- Daily quick run: 5-10 critical cases.
- Weekly full run: 30+ mixed difficulty cases.
- Release gate: passRate target >= 0.80 on full run.

## 6) Admin runner export
The admin page `/admin/rag` supports result export after compare run:
- Export JSON: full compare payload (good for audit/history)
- Export CSV: flattened per-case rows across profiles (good for spreadsheet review)

## 7) Admin runner history
The admin page `/admin/rag` now keeps the most recent 20 benchmark runs in browser localStorage.
- Purpose: quickly reuse compare parameters and retain context across refresh
- Scope: local to the browser profile (not server-shared)
- Controls: reusable parameter action + clear history button

## 8) Shared backend history (admin)
Backend now persists compare runs to system settings for cross-session admin access.

Endpoints:
- GET `/api/ai/benchmark-history` (Admin)
- GET `/api/ai/benchmark-history/analytics` (Admin)
- DELETE `/api/ai/benchmark-history` (Admin)

Notes:
- Maximum persisted history: 20 runs
- Stored payload includes profile leaderboard summaries, run metadata, and compare input snapshot (profiles/topK/maxContextChars/semanticThreshold/cases)
- Admin UI supports filter/search (profile/date/min-cases/text)
- Admin can load a shared run back into the benchmark page for quick inspection/re-compare
- Shared history load now restores benchmark parameters and expected keywords for full replay fidelity
- Older history entries created before this change may still fall back to reconstructed empty keyword lists

Analytics endpoint highlights:
- Per-profile metrics: average pass rate, pass-rate standard deviation, stability score
- Drift tracking: compares recent window vs baseline window and emits drift flag
- Operational fields: total runs, latest run timestamp, recent/baseline window sizes
