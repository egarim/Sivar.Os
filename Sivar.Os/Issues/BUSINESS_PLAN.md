# Sivar.Os Business Plan

## Executive Summary

**Sivar.Os** is an AI-powered local discovery and community platform designed specifically for El Salvador. It combines semantic search, AI chat assistants, and business directory features to help users find local businesses, government services, tourism information, and community content. The platform aims to become the digital hub for Salvadorans, connecting citizens with essential services, local businesses, and each other.

---

## Product Overview

### Core Platform Features

1. **AI-Powered Search & Chat**
   - Conversational AI assistant for natural language queries
   - Semantic search using vector embeddings (OpenAI/Ollama)
   - Multilingual support (Spanish/English) with smart query translation
   - Intent classification for routing queries to appropriate search domains

2. **Business & Service Directory**
   - Restaurant, retail, and service listings with rich metadata
   - Location-based search with PostGIS geo-queries
   - Business contact info, hours, and service details
   - Product and service catalogs

3. **Government Procedures Hub**
   - Step-by-step guides for common procedures (DUI, passport, NIT, licenses)
   - Requirements, costs, processing times, and locations
   - Direct links to government portals and appointment systems

4. **Tourism & Entertainment**
   - Local attractions, events, and entertainment venues
   - Tour guides and experiences
   - Cultural and recreational information

5. **Social & Community Features**
   - User profiles (Personal, Business, Government)
   - Posts, reactions, comments, and bookmarks
   - Follower/following relationships
   - Activity feeds with Elo-based content ranking

---

## Target Market

### Primary Market: El Salvador
- **Population**: ~6.5 million
- **Internet Users**: ~3.5 million (54% penetration)
- **Mobile Users**: 80%+ smartphone adoption
- **Diaspora**: ~2.5 million Salvadorans abroad (USA, primarily)

### User Segments

| Segment | Description | Value Proposition |
|---------|-------------|-------------------|
| **Consumers** | Local citizens seeking businesses/services | Find anything in one place with AI assistance |
| **Small Businesses** | Restaurants, shops, service providers | Affordable digital presence and customer acquisition |
| **Government** | Ministries, municipalities, agencies | Citizen services portal and procedure documentation |
| **Tourists** | Domestic and international visitors | Local discovery and trip planning |
| **Diaspora** | Salvadorans abroad | Stay connected, find services for family, plan visits |

---

## Revenue Streams

### 1. Sponsored Listings (Pay-Per-Click Ads) 💰
**Primary Revenue Driver**

The platform implements a **second-price auction model** similar to Google Ads:

| Component | Description |
|-----------|-------------|
| **Ad Budget** | Businesses deposit credit for sponsored placements |
| **Max Bid Per Click** | Set maximum CPC (default: $0.10) |
| **Daily Limit** | Cap daily spend (default: $5.00) |
| **Quality Score** | CTR-based scoring (0.1-1.0) affects ad position |
| **Target Keywords** | Businesses can target specific search terms |
| **Geo-Targeting** | Target users within a radius |

**Revenue Mechanics:**
- Businesses pay only when users click (CPC model)
- Second-price auction ensures fair pricing
- Quality score rewards engaging businesses with lower costs
- Ad transactions tracked for full audit trail

**Projected Revenue:**
- 1,000 active advertisers × $100/month avg = **$100,000/month**
- Conservative: $50,000/month (500 advertisers)
- Growth target: $500,000/month (5,000 advertisers at scale)

---

### 2. Token Allowance Subscriptions 🔑
**AI Chat Usage Tiers**

Each profile has a token allowance for AI chat interactions:

| Tier | Token Limit | Period | Price | Target User |
|------|-------------|--------|-------|-------------|
| **Free** | 100,000 tokens | Monthly | $0 | Casual users |
| **Pro** | 500,000 tokens | Monthly | $9.99 | Power users |
| **Business** | 2,000,000 tokens | Monthly | $29.99 | SMBs |
| **Enterprise** | Unlimited | Monthly | Custom | Large orgs |

**Revenue Potential:**
- 10,000 Pro subscribers = **$99,900/month**
- 1,000 Business subscribers = **$29,990/month**

---

### 3. Premium Business Profiles 🏢
**Enhanced Business Features**

| Feature | Free | Premium ($19.99/mo) | Enterprise ($99.99/mo) |
|---------|------|---------------------|------------------------|
| Basic listing | ✅ | ✅ | ✅ |
| Contact info display | ✅ | ✅ | ✅ |
| Multiple profiles | 1 | 5 | Unlimited |
| Analytics dashboard | ❌ | ✅ | ✅ |
| Featured in category | ❌ | ✅ | ✅ |
| Custom chatbot persona | ❌ | ❌ | ✅ |
| API access | ❌ | ❌ | ✅ |
| Priority support | ❌ | ✅ | ✅ |
| Verified badge | ❌ | ✅ | ✅ |

---

### 4. Lead Generation (Future) 📊
**Pay-Per-Lead Model**

- Track user actions: calls, directions, website visits
- Charge per qualified lead (higher than CPC)
- Ideal for high-value services (lawyers, doctors, real estate)

---

### 5. Government Partnership (B2G) 🏛️
**Digital Government Services**

- Partner with government ministries for procedure documentation
- White-label solutions for municipal portals
- Citizen feedback and analytics for public services
- Potential government grant/contract revenue

---

### 6. Data & Analytics Services 📈
**Aggregate Insights (Privacy-Compliant)**

- Search trend reports by category/location
- Consumer behavior insights for businesses
- Market research for investors/NGOs

---

## Revenue Model Summary

| Revenue Stream | Year 1 Target | Year 2 Target | Year 3 Target |
|----------------|---------------|---------------|---------------|
| Sponsored Listings | $300,000 | $1,200,000 | $3,600,000 |
| Token Subscriptions | $60,000 | $240,000 | $600,000 |
| Premium Profiles | $50,000 | $200,000 | $500,000 |
| B2G Partnerships | $25,000 | $150,000 | $400,000 |
| **Total ARR** | **$435,000** | **$1,790,000** | **$5,100,000** |

---

## Go-Live Plan

### Phase 1: Soft Launch (Weeks 1-4) 🚀
**Objective:** Validate core functionality with real users

| Task | Week | Owner | Success Metric |
|------|------|-------|----------------|
| Deploy to production environment | 1 | DevOps | 99.9% uptime |
| Seed 500+ real business profiles | 1-2 | Operations | Profiles verified |
| Invite 50 beta testers (friends/family) | 2 | Marketing | 50 active users |
| Complete government procedure content | 2-3 | Content | 20+ procedures |
| Fix critical bugs from feedback | 3-4 | Engineering | <5 critical bugs |
| Tune AI prompts and search quality | 3-4 | AI Team | 80%+ query satisfaction |

**Key Deliverables:**
- [ ] Production deployment on Azure/AWS
- [ ] PostgreSQL + pgvector database configured
- [ ] Keycloak authentication live
- [ ] CDN for static assets
- [ ] Monitoring and alerting

---

### Phase 2: Private Beta (Weeks 5-8) 🧪
**Objective:** Scale to 500+ users, validate monetization

| Task | Week | Owner | Success Metric |
|------|------|-------|----------------|
| Onboard 100 local businesses | 5-6 | Sales | Business signups |
| Enable sponsored listings | 5 | Engineering | Ads serving |
| Launch referral program | 6 | Marketing | 2x user growth |
| A/B test onboarding flows | 6-7 | Product | 40%+ activation |
| First paying advertisers | 7-8 | Sales | 10+ advertisers |
| Performance optimization | 7-8 | Engineering | <2s page load |

**Marketing Activities:**
- Instagram/TikTok launch videos
- WhatsApp broadcast to initial network
- Local business outreach (door-to-door in key areas)
- Press release to tech media in El Salvador

---

### Phase 3: Public Launch (Weeks 9-12) 🎉
**Objective:** Go fully public, aggressive user acquisition

| Task | Week | Owner | Success Metric |
|------|------|-------|----------------|
| Public announcement | 9 | Marketing | 1,000 signups in 48hrs |
| Launch influencer partnerships | 9-10 | Marketing | 5 influencers onboard |
| Radio/TV mentions | 10-11 | PR | 3 media appearances |
| Google Play / App Store listing (PWA) | 10 | Engineering | App listed |
| Launch premium subscriptions | 11 | Product | 50 paying subs |
| 24/7 customer support | 12 | Support | <1hr response time |

**Launch Event:**
- Virtual launch event with demos
- Giveaways (free premium for early adopters)
- Partnership announcements

---

### Phase 4: Growth & Expansion (Months 4-12) 📈
**Objective:** Achieve product-market fit, scale revenue

| Milestone | Target Date | Goal |
|-----------|-------------|------|
| 5,000 monthly active users | Month 4 | Community growth |
| 500 business profiles | Month 4 | Supply side |
| $10,000 MRR | Month 6 | Revenue milestone |
| 20,000 MAU | Month 8 | User growth |
| $50,000 MRR | Month 10 | Scale revenue |
| Break-even operations | Month 12 | Sustainability |

**Expansion Plans:**
- Mobile native apps (iOS/Android)
- WhatsApp integration for chat
- B2G partnerships with municipalities
- Expand to Guatemala/Honduras (similar markets)

---

## Technical Go-Live Checklist

### Infrastructure
- [ ] Production environment deployed (Azure/AWS/DO)
- [ ] PostgreSQL with pgvector extension configured
- [ ] Redis cache for sessions
- [ ] Azure Blob Storage for media
- [ ] CDN configured (Cloudflare/Azure CDN)
- [ ] SSL certificates installed
- [ ] Domain configured (sivar.os or sivaros.app)

### Security
- [ ] Keycloak SSO production instance
- [ ] Rate limiting enabled
- [ ] WAF configured
- [ ] CORS properly configured
- [ ] Secrets in Azure Key Vault / AWS Secrets Manager
- [ ] GDPR/privacy compliance reviewed

### Monitoring & Operations
- [ ] Application Insights / Sentry for error tracking
- [ ] Log aggregation (Seq/Elasticsearch)
- [ ] Uptime monitoring (Pingdom/UptimeRobot)
- [ ] Database backup automation
- [ ] Disaster recovery plan documented
- [ ] Runbook for common operations

### Performance
- [ ] Load testing completed (1,000+ concurrent users)
- [ ] Database indexes optimized
- [ ] Response times <500ms for API calls
- [ ] WebAssembly bundle optimized (<3MB)
- [ ] Image optimization pipeline

### Content
- [ ] 500+ business profiles seeded
- [ ] 20+ government procedures documented
- [ ] 100+ restaurant listings
- [ ] 50+ tourism attractions
- [ ] AI prompts tested and refined
- [ ] Embedding vectors generated for all content

---

## Key Metrics to Track

### Product Metrics
| Metric | Definition | Target (Month 6) |
|--------|------------|------------------|
| MAU | Monthly Active Users | 10,000 |
| DAU | Daily Active Users | 2,000 |
| Queries/User | Avg searches per session | 5 |
| Search Success | Queries with clicks | 60% |
| Retention | 7-day retention | 30% |

### Revenue Metrics
| Metric | Definition | Target (Month 6) |
|--------|------------|------------------|
| MRR | Monthly Recurring Revenue | $10,000 |
| ARPU | Avg Revenue Per User | $0.50 |
| CAC | Customer Acquisition Cost | <$5 |
| LTV | Lifetime Value | >$25 |
| Ad Fill Rate | % searches with ads | 20% |

---

## Competitive Advantage

1. **AI-First Experience**: Natural language search vs. traditional filters
2. **Local Focus**: Built specifically for El Salvador's unique needs
3. **Government Integration**: Only platform with procedure guides
4. **Bilingual by Design**: Native Spanish/English support
5. **Modern Tech Stack**: Blazor + AI gives competitive edge
6. **Community Features**: Social layer competitors lack
7. **Fair Advertising**: Quality-based auctions favor good businesses

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Low adoption | High | Free tier, aggressive marketing |
| AI costs spike | Medium | Token limits, caching, local models (Ollama) |
| Competition | Medium | Move fast, build local relationships |
| Payment processing | Medium | Partner with local banks, support crypto |
| Content quality | Medium | Manual curation, community reporting |
| Technical issues | High | Robust monitoring, quick iteration |

---

## AI Model Cost Analysis & Optimization 🤖💰

### OpenAI Pricing Reference (December 2024)

#### Chat/Completion Models (per 1M tokens)

| Model | Input | Output | Best For |
|-------|-------|--------|----------|
| **gpt-4o-mini** | $0.15 | $0.60 | ⭐ **Recommended** - Best cost/performance |
| gpt-4o | $2.50 | $10.00 | Complex reasoning (expensive) |
| gpt-4.1-mini | $0.40 | $1.60 | Newer alternative |
| gpt-4.1-nano | $0.10 | $0.40 | Ultra-cheap, simpler tasks |
| gpt-3.5-turbo | $0.50 | $1.50 | Legacy, still viable |

#### Embedding Models (per 1M tokens)

| Model | Standard | Batch | Dimensions |
|-------|----------|-------|------------|
| **text-embedding-3-small** | $0.02 | $0.01 | 1536 ⭐ **Recommended** |
| text-embedding-3-large | $0.13 | $0.065 | 3072 |
| text-embedding-ada-002 | $0.10 | $0.05 | 1536 (legacy) |

---

### Recommended Model Strategy

#### Phase 1: Cost-Optimized Launch
```
Chat Model:     gpt-4o-mini ($0.15/$0.60 per 1M tokens)
Embeddings:     text-embedding-3-small ($0.02 per 1M tokens)
Fallback:       Ollama (local, free) for development/testing
```

#### Phase 2: Hybrid Approach (Scale)
```
Simple queries: gpt-4o-mini (80% of traffic)
Complex queries: gpt-4o (20% of traffic) - when intent requires reasoning
Embeddings:     text-embedding-3-small (all traffic)
Batch jobs:     Use Batch API for 50% discount on non-urgent tasks
```

#### Phase 3: Enterprise (Future)
```
Consider:       Fine-tuned gpt-4o-mini for domain-specific responses
Local models:   Ollama with Llama 3.2 for on-premise deployments
```

---

### Token Usage Estimation

#### Per-Query Token Breakdown

| Component | Tokens | Notes |
|-----------|--------|-------|
| System prompt | ~500 | Agent instructions, persona |
| User query | ~50 | Average search query |
| Search context | ~1,000 | Retrieved results for context |
| Function calls | ~200 | Tool definitions |
| Response | ~300 | Average assistant reply |
| **Total per query** | **~2,050** | Input + Output |

**Breakdown:**
- Input tokens: ~1,750 (system + query + context + functions)
- Output tokens: ~300 (response)

#### Embedding Token Usage

| Operation | Tokens | Frequency |
|-----------|--------|-----------|
| Query embedding | ~50 | Every search |
| Profile embedding | ~500 | On create/update |
| Post embedding | ~300 | On create/update |

---

### Monthly Cost Projections

#### Scenario 1: Early Stage (1,000 MAU)

| Metric | Value |
|--------|-------|
| Active users | 1,000 |
| Queries per user/month | 20 |
| Total queries/month | 20,000 |
| Tokens per query | 2,050 |
| **Total tokens** | **41M tokens** |

**Cost Calculation (gpt-4o-mini):**
| Component | Tokens | Rate | Cost |
|-----------|--------|------|------|
| Input tokens | 35M | $0.15/1M | $5.25 |
| Output tokens | 6M | $0.60/1M | $3.60 |
| Embeddings | 1M | $0.02/1M | $0.02 |
| **Monthly Total** | | | **$8.87** |

---

#### Scenario 2: Growth Stage (10,000 MAU)

| Metric | Value |
|--------|-------|
| Active users | 10,000 |
| Queries per user/month | 25 |
| Total queries/month | 250,000 |
| Tokens per query | 2,050 |
| **Total tokens** | **512M tokens** |

**Cost Calculation (gpt-4o-mini):**
| Component | Tokens | Rate | Cost |
|-----------|--------|------|------|
| Input tokens | 437M | $0.15/1M | $65.55 |
| Output tokens | 75M | $0.60/1M | $45.00 |
| Embeddings | 12M | $0.02/1M | $0.24 |
| **Monthly Total** | | | **$110.79** |

---

#### Scenario 3: Scale Stage (50,000 MAU)

| Metric | Value |
|--------|-------|
| Active users | 50,000 |
| Queries per user/month | 30 |
| Total queries/month | 1,500,000 |
| Tokens per query | 2,050 |
| **Total tokens** | **3.075B tokens** |

**Cost Calculation (gpt-4o-mini):**
| Component | Tokens | Rate | Cost |
|-----------|--------|------|------|
| Input tokens | 2.625B | $0.15/1M | $393.75 |
| Output tokens | 450M | $0.60/1M | $270.00 |
| Embeddings | 75M | $0.02/1M | $1.50 |
| **Monthly Total** | | | **$665.25** |

---

#### Scenario 4: Enterprise (100,000+ MAU)

| Metric | Value |
|--------|-------|
| Active users | 100,000 |
| Queries per user/month | 35 |
| Total queries/month | 3,500,000 |
| Tokens per query | 2,050 |
| **Total tokens** | **7.175B tokens** |

**Cost Calculation (gpt-4o-mini):**
| Component | Tokens | Rate | Cost |
|-----------|--------|------|------|
| Input tokens | 6.125B | $0.15/1M | $918.75 |
| Output tokens | 1.05B | $0.60/1M | $630.00 |
| Embeddings | 175M | $0.02/1M | $3.50 |
| **Monthly Total** | | | **$1,552.25** |

---

### Cost Comparison: Model Choices

| MAU | gpt-4o-mini | gpt-4o | gpt-4.1-nano | Savings w/ mini |
|-----|-------------|--------|--------------|-----------------|
| 1,000 | $9 | $128 | $5 | 93% vs gpt-4o |
| 10,000 | $111 | $1,594 | $62 | 93% vs gpt-4o |
| 50,000 | $665 | $9,563 | $369 | 93% vs gpt-4o |
| 100,000 | $1,552 | $22,313 | $862 | 93% vs gpt-4o |

**Key Insight:** Using `gpt-4o-mini` instead of `gpt-4o` saves **93%** on AI costs with minimal quality impact for search/chat use cases.

---

### Cost Optimization Strategies

#### 1. Prompt Caching (Save 40-60%)
```
- Cache system prompts that don't change
- Reuse context for follow-up queries in same session
- OpenAI offers automatic prompt caching for identical prefixes
```

#### 2. Response Caching (Save 30-50%)
```
- Cache common queries (e.g., "pizza near me")
- Use semantic similarity to match cached responses
- TTL: 1-24 hours based on query type
```

#### 3. Batch API (Save 50%)
```
- Use for non-real-time tasks:
  - Generating embeddings for new content
  - Daily summary generation
  - Content moderation
- $0.075 input / $0.30 output (vs $0.15/$0.60)
```

#### 4. Token Limit Enforcement
```
- Free tier: 100,000 tokens/month (~50 queries)
- Enforce limits in ProfileService
- Upsell to premium when limit reached
```

#### 5. Hybrid Local/Cloud
```
- Development: Ollama (free, local)
- Low-priority: Ollama fallback
- Production: OpenAI for quality
- Cost: $0 for 30-40% of requests
```

#### 6. Smart Routing
```
- Simple intents → gpt-4.1-nano ($0.10/$0.40)
- Search queries → gpt-4o-mini ($0.15/$0.60)
- Complex reasoning → gpt-4o ($2.50/$10.00)
- Potential savings: 20-30% with intent-based routing
```

---

### Cost vs Revenue Analysis

| Stage | MAU | AI Cost/Mo | Revenue/Mo | AI as % Revenue | Margin |
|-------|-----|------------|------------|-----------------|--------|
| Early | 1,000 | $9 | $1,000 | 0.9% | 99.1% |
| Growth | 10,000 | $111 | $15,000 | 0.7% | 99.3% |
| Scale | 50,000 | $665 | $75,000 | 0.9% | 99.1% |
| Enterprise | 100,000 | $1,552 | $150,000 | 1.0% | 99.0% |

**Key Insight:** AI costs remain **under 1% of revenue** at all stages when using `gpt-4o-mini`. This is extremely cost-efficient compared to human customer support or traditional search infrastructure.

---

### Break-Even Analysis per User

| Cost Component | Per User/Month |
|----------------|----------------|
| AI (25 queries × $0.001/query) | $0.025 |
| Infrastructure (est.) | $0.02 |
| Support (amortized) | $0.01 |
| **Total Cost per User** | **$0.055** |
| **Required ARPU for 50% margin** | **$0.11** |

**With advertising revenue of $0.50+ ARPU, margins are highly attractive.**

---

### Recommended Monthly AI Budget

| Phase | Timeline | Budget | Expected MAU |
|-------|----------|--------|--------------|
| Soft Launch | Months 1-2 | $50 | 500 |
| Beta | Months 3-4 | $100 | 2,000 |
| Public Launch | Months 5-6 | $200 | 5,000 |
| Growth | Months 7-9 | $500 | 15,000 |
| Scale | Months 10-12 | $1,500 | 50,000 |
| **Year 1 Total** | | **~$7,000** | |

---

## Team Requirements (Go-Live)

| Role | Count | Responsibility |
|------|-------|----------------|
| Full-Stack Developer | 2 | Platform development |
| AI/ML Engineer | 1 | Search quality, embeddings |
| DevOps | 1 | Infrastructure, deployment |
| Product Manager | 1 | Roadmap, features |
| Marketing/Sales | 1 | User acquisition, business sales |
| Customer Support | 1 | User issues, business onboarding |
| Content Creator | 1 | Procedure docs, marketing content |

**Total: 8 people for launch**

---

## Funding Requirements

### Pre-Launch (Months 1-3)
| Category | Monthly Cost | Total |
|----------|--------------|-------|
| Team (reduced salaries) | $15,000 | $45,000 |
| Infrastructure | $500 | $1,500 |
| AI API costs | $300 | $900 |
| Marketing | $1,000 | $3,000 |
| **Total** | | **$50,400** |

### Post-Launch (Months 4-12)
| Category | Monthly Cost | Total |
|----------|--------------|-------|
| Team (full salaries) | $30,000 | $270,000 |
| Infrastructure | $2,000 | $18,000 |
| AI API costs | $1,500 | $13,500 |
| Marketing | $5,000 | $45,000 |
| **Total** | | **$346,500** |

### First Year Total: ~$400,000

---

## Conclusion

Sivar.Os is positioned to become El Salvador's leading local discovery platform by combining:

1. **AI-powered search** that understands natural language
2. **Comprehensive directory** of businesses, services, and government procedures
3. **Fair monetization** through quality-based advertising
4. **Scalable technology** built on modern .NET 9 + Blazor stack

With focused execution on the go-live plan and disciplined pursuit of the identified revenue streams, Sivar.Os can achieve profitability within 12-18 months and establish itself as critical infrastructure for El Salvador's digital economy.

---

**Next Steps:**
1. Finalize production infrastructure
2. Complete business profile seeding
3. Execute soft launch with beta testers
4. Iterate based on feedback
5. Scale marketing for public launch

*Document Version: 1.0 | Created: December 2024*
