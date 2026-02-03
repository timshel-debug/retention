# Implementation Plan (API)

1. API skeleton + health + swagger (REQ-0003/REQ-0004/REQ-0005).
2. DTOs + boundary validation + request size limits (REQ-0006).
3. ProblemDetails middleware + error_code mapping (REQ-0007).
4. Validate endpoint with deterministic errors/warnings (REQ-0002).
5. Evaluate endpoint (REQ-0001) + deterministic decision log (REQ-0008).
6. Optional toggles: auth (REQ-0009) and rate limiting (REQ-0010).
7. Observability spans/metrics (NFR-0003) with ActivityListener tests.
