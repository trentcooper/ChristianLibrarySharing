# Bulk Book Onboarding — Design Brief

> **Status:** Design / Post-MVP
> **Problem:** Initial library setup is the biggest adoption risk. A user with 500–2,000 books will not type them in one at a time. The onboarding experience has to make a large library feel fast — ideally magical — to convert.

---

## Guiding Principle: Photo First

The standard instinct is "easy methods first, fancy later." Here we invert that deliberately.

The emotional moment at signup is: *"I have 1,500 books and now I have to enter them all?"* Even barcode scanning — which is fast — still requires picking up each book, finding the barcode, and scanning it. That's 1,500 individual interactions. Mentally exhausting.

If the user's **first** experience is "point your phone at your shelf and watch 50 books appear in 30 seconds," the psychology changes entirely. Even at 60–70% accuracy, the user is engaged, sees the platform as magical, and the remaining work feels like *cleanup* rather than *data entry*. The other methods (barcode, search, manual) become finishing tools, not the whole job.

This is a case where a worse technical solution (vision is less accurate than ISBN scanning) produces a better user experience because of *when* it appears in the flow. A first pass that captures even 40–80% of a shelf is enough to keep a user engaged.

---

## Onboarding Methods (in intended sequence)

### 1. Shelf photo scanning (the headline feature)
User photographs their shelves; a vision model identifies spines and returns probable books. See the deep dive below.

### 2. ISBN barcode scanning via phone camera
The single biggest *reliable* unlock. 2–4 seconds per book, no typing. In-browser options like QuaggaJS or ZXing; native mobile is faster. Each scan triggers a metadata lookup (Open Library, Google Books, ISBNdb). Turns a 40-hour task into ~2–3 hours spread over evenings.

### 3. CSV import from existing platforms
Many serious readers already keep their library in Goodreads, LibraryThing, or StoryGraph. Goodreads CSV export (ISBN, title, author, shelves, rating, date read) is the most common; supporting that one format likely covers 60%+ of power users.

### 4. Bulk ISBN paste
A textarea where users paste ISBNs (one per line or comma-separated) for batch lookup. Useful for users who already have a spreadsheet.

### 5. AI-assisted natural-language batch entry
User pastes something like *"Mere Christianity, The Cost of Discipleship, Knowing God by J.I. Packer, the Tim Keller marriage book"* and an LLM parses it into structured queries matched against a book database. Handles the "I know the titles but not the ISBNs" case well.

---

## Shelf Photo Scanning — Deep Dive

### Three approaches

**Approach 1 — Pure vision model.** Send the photo to a vision-capable model with a prompt like "List every book visible with title and author when readable." Returns structured data, which you match against a book database to enrich with ISBN, cover, publisher.
- *Pros:* Handles rotated text, partial occlusion, weird fonts, faded spines. Improves automatically as models improve.
- *Cons:* Per-call cost. Slower than dedicated OCR. Hallucination risk (a plausible-sounding book that doesn't exist).

**Approach 2 — OCR + matching.** Dedicated OCR (Google Vision, Azure Computer Vision, Tesseract) extracts raw text, then fuzzy-match against a book database.
- *Pros:* Cheaper, faster, deterministic.
- *Cons:* OCR struggles with vertical spine text, decorative fonts, and metallic/embossed lettering common on Christian books. Matching needs careful tuning.

**Approach 3 — Hybrid.** OCR for easy cases, vision model for hard cases or confirmation. Or: vision model to identify spine regions (bounding boxes), then OCR each region.

**Recommendation for MVP: Approach 1.** Christian books often have stylized fonts, gold lettering, and older typography that wreck traditional OCR. A vision model handles those gracefully and gives a forgiving development path. Optimize toward OCR later if cost becomes an issue.

### End-to-end flow

1. User snaps a photo (or several, panning across shelves).
2. Photo uploaded to backend; **queued for async processing** (no synchronous wait).
3. Vision model extracts probable books — title, author, confidence score each.
4. For each extraction, query the metadata service (Open Library, Google Books) for a canonical match.
5. Present results in a **review UI**: a grid of cards showing the spine photo region next to the matched book, with a confidence indicator.
6. User confirms / edits / rejects each — bulk-accept high-confidence matches.
7. Unmatched / low-confidence books go to a "couldn't identify" list, resolved later via barcode or manual entry.

The review step is critical. Don't auto-add everything — the user is the final arbiter, but confirmation should be a single tap for high-confidence matches. Tapping "yes, yes, yes, edit-this-one, yes" through 50 books in two minutes feels great; silent auto-adds with wrong books feel terrible.

### Practical tips
- **Image quality guidance.** A short overlay tutorial: shelf well-lit, camera parallel to spines, one shelf at a time. Bad photos → bad results → blame on the app.
- **Multiple shots, one library.** Let users add photos incrementally, shelf by shelf. Don't require photographing the whole library in one session.
- **Confidence thresholds drive UI.** Auto-pre-select high-confidence, flag medium for review, surface low as "we saw something here but couldn't identify it."
- **Cache aggressively.** If two users photograph the same edition, the second lookup is instant. The metadata store becomes a community asset.
- **Cost modeling.** A 1,500-book user might generate 30–50 photos. At a few cents per call, ~$1–2 per onboarding — fine if conversion lift justifies it, painful on a free tier at scale. Model this early.

### Simplest MVP version
Single photo upload → vision API with a structured-output prompt → list of candidates → review UI. Skip bounding-box visualization, multi-shelf orchestration, and confidence-based auto-acceptance. Prove the flow end-to-end with one shelf, watch real users, then invest where the real bottleneck is (image quality, accuracy, review UI, or metadata matching). Optimizing ahead of usage data is almost always wrong.

---

## Christian-Library-Specific Considerations

A meaningful chunk of Christian books — older Puritan reprints, smaller publishers, self-published material — have spotty metadata in mainstream databases.

- **Fallback flow when an ISBN lookup returns nothing:** let the user enter manually, then cache the result so the next person who scans the same ISBN gets it instantly. The platform becomes its own metadata source over time.
- **Curated bulk-add lists:** "Add all 18 Puritan Paperbacks," "Add Tim Keller's complete works," "Add the ESV Study Bible." For a focused audience, owning significant overlap with these sets makes curated adds a big win.

---

## Architecture Notes

The onboarding flow is a good case study for the patterns under review.

- **Metadata lookup as a separate bounded context.** It has different scaling needs from the rest of the platform — read-heavy, cacheable, external-API-dependent. Warrants its own cache, possibly its own service, or a serverless function if lookup volume is bursty.
- **Bulk imports are inherently asynchronous.** Don't make a user wait for 1,500 API calls inside a single HTTP request. Natural fit for the message-queue / background-worker pattern, and a place to apply saga thinking if any part can fail mid-import.
- **Idempotency.** If a user re-uploads their Goodreads CSV by accident, you don't want 3,000 books. Design imports with idempotency keys — often the ISBN scoped to the user — up front.

---

## Suggested Backlog (Post-MVP)

| Story | Method | Notes |
|-------|--------|-------|
| Shelf photo extraction (vision) | Photo first | Headline feature; MVP = single shelf, review UI |
| ISBN barcode scanning | Barcode | High reliability; in-browser + native |
| Goodreads CSV import | CSV | Covers most power users; idempotent |
| Bulk ISBN paste | Paste | Batch lookup from textarea |
| AI natural-language batch entry | Text → structured | "Titles but no ISBNs" case |
| Metadata service + cache | Infra | Bounded context; community metadata asset |
| Curated bulk-add lists | Curated | Puritan Paperbacks, complete-works sets |

*Tag candidates: `Post-MVP + Technical Debt`; `Architecture` for the metadata-service/bounded-context work.*
