# ADR 0001 — Soft-delete cascade for parent/child aggregates

Date: 2026-09-07 · Status: accepted · Decided during Task 2.1 (first aggregate with children).

## Context
Every aggregate is soft-deleted (`BaseEntity.DeletedAt`, global query filter, `StampInterceptor`
turns `Remove` into an update). The database still has real foreign keys with cascade or restrict,
but a soft delete never fires them, so the behaviour for children has to be decided in code.

## Decision
- **Owned children follow the parent.** Rows that have no meaning without the parent (event
  gallery images, event results; later: page blocks, training-guide chapters, news images) are
  soft-deleted in the same `SaveChanges` by the aggregate's service (`EventsService.DeleteAsync`).
  The database FK is `Cascade` so a future hard purge behaves the same way.
- **Records that belong to a person stay.** `EventRegistration` rows are CRM data: an event that
  an editor removes must not make a visitor's entry disappear from the CRM. They are never
  cascaded; the FK is `Restrict`, and the CRM lists them with the parent marked deleted.
- Restoring a parent (Week 4 dashboard) restores the children it cascaded, by matching
  `DeletedAt` timestamps within the same second.
- Children carry the global filter too, so a child is never returned for a deleted parent even
  when queried directly.

## Consequences
Services own the cascade, not the interceptor; a new aggregate with children must add it to its
`DeleteAsync` and cover it with a test like `EventsModelTests.Deleting_an_event_soft_deletes_…`.
