# Migration Guide

## 0. prologue
### 1. ASP.NET 4.8 is the last version which support WebForm. 
- 

### 2. Software Infras-structure 
- VM, Docker, K8S, HA, Loadbalancing, Server farm, cluster, master/slave etc 

### 3. New programming paradigm 
- Angular, React, Vue, Restful-API etc 


## I. Target servers
- **prod-rw**: Read/Write server
- **prod-ro**: Read only server

## II. Impact analysys 
- Level 0: Target based, no need to do anything
- Level 1: Minimal retrofit required (Read only)
- Level 2: Minimal retrofit required (Read/Write)
- Level 3: Moderate retrofit required (Read only)
- Level 4: Moderate retrofit required (Read/Write)
- Level 5: Extensive retrofit required (Read only)
- Level 6: Extensive retrofit required (Read/Write)
- Level 7: Verbatim re-write, can do Read/Write Splitting (讀寫分離)
- Level 8: Verbatim re-write, can't do Read/Write Splitting (讀寫分離)
---
- Minimal : change of connection string and configuration
- Moderate : change of one or more SQL statements
- Extensive : change of source code 
- Verbatim: Line-by-line re-write from one language to another


## III. Action plan
- Phase 1: Analyse program sources and wipe off used programs. (3 months)
- Phase 2: Infra-structure ie. servers setup and synchronization mechanisms.  (3 months)
- Phase 3: Level 1 & Level 2 (3 months)
- Phase 4: Level 3 & Level 4 (3 months)
- Phase 5: Level 5 & Level 5 (3 months)
- Phase 6: Level 7 & Level 8 (3 months)

## IV. Retrospect

## Appendix 

## EOF (2022/08/30)