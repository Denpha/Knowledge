# LINE OA Incident Runbook

## 1) Scope
This runbook covers incident response for LINE OA webhook ingestion and reply flow in KMS.

Endpoints:
- GET /api/line/webhook
- GET /api/line/webhook/telemetry
- POST /api/line/webhook

Core protections already implemented:
- Signature verification
- Event dedup (in-memory + persistent marker)
- Inbound rate limiting
- Reply retry/backoff
- Threshold-based telemetry alerts
- Optional external alert webhook

## 2) Service Level Targets (Operational)
- Webhook availability: >= 99.9%
- Reply failure ratio (window): < 3%
- Processing error ratio (window): < 3%
- Rate-limited ratio (window): < 10%

Critical trigger (immediate response):
- Reply failure ratio > 10%
- Processing error ratio > 10%
- Rate-limited ratio > 25%

## 3) Quick Health Check
1. Open admin settings and inspect LINE Observability panel.
2. Validate GET /api/line/webhook returns enabled/config readiness.
3. Validate GET /api/line/webhook/telemetry metrics:
   - processedEvents
   - duplicateEvents
   - rateLimitedEvents
   - replyFailures
   - processingErrors
4. Check API logs for:
   - "LINE alert threshold reached"
   - "LINE reply failed"
   - "Invalid LINE signature"

## 4) Incident Severity
- SEV-1: No replies to most incoming messages, sustained > 10 minutes
- SEV-2: Partial degradation, high retry/error/rate-limit pressure
- SEV-3: Intermittent alerts, no clear user impact yet

## 5) Triage Playbook
### A. Signature failures spike
Likely causes:
- Wrong ChannelSecret
- Proxy/body mutation

Actions:
1. Verify LineOA.ChannelSecret in environment/appsettings.
2. Confirm raw body is not rewritten by middleware/proxy.
3. Replay sample webhook payload with valid signature.

### B. Reply failures spike
Likely causes:
- Invalid/expired channel token
- LINE API latency or upstream outage

Actions:
1. Verify LineOA.ChannelAccessToken.
2. Check response body/status from LINE API in logs.
3. Temporarily reduce traffic pressure (tighten inbound limits).
4. If external outage, keep service up and communicate degraded state.

### C. Rate-limited events spike
Likely causes:
- Bot spam or traffic burst
- Threshold too strict

Actions:
1. Review source distribution (user/group/room) in logs.
2. Increase InboundRateLimitMaxMessages cautiously.
3. Keep cooldown and alert thresholds active.

### D. Duplicate events spike
Likely causes:
- Upstream retries due to timeout
- Slow processing path

Actions:
1. Verify webhook response timing.
2. Check persistent dedup marker cleanup health.
3. Validate DB connectivity for SystemSettings writes.

## 6) Configuration Controls
LINE OA keys:
- Enabled
- ChannelSecret
- ChannelAccessToken
- EventDedupWindowMinutes
- ReplyMaxRetries
- ReplyRetryBaseDelayMs
- InboundRateLimitWindowSeconds
- InboundRateLimitMaxMessages
- TelemetryWindowSeconds
- AlertDuplicateEventsPerWindow
- AlertRateLimitedEventsPerWindow
- AlertReplyFailuresPerWindow
- AlertProcessingErrorsPerWindow
- EnableExternalAlerts
- ExternalAlertWebhookUrl
- ExternalAlertCooldownSeconds

## 7) Escalation Flow
1. First responder acknowledges within 5 minutes.
2. If SEV-1 or unresolved > 15 minutes, escalate to backend owner.
3. If external platform issue suspected, notify stakeholder channel and switch to status updates every 15 minutes.
4. Close incident with root cause, mitigation, and follow-up tasks.

## 8) Post-Incident Checklist
- Capture timeline (detect, mitigate, recover).
- Record failed thresholds and observed ratios.
- Add/update regression test scenario.
- Adjust thresholds if false-positive or too noisy.
- Update this runbook with new learnings.
