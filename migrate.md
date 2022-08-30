# Migration Guide

## I. Target servers
- prod-rw: Read/Write server
- prod-ro: Read only server

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
- Phase 1: Infra-structure ie. servers setup and synchronization mechanisms. 
- Phase 2: Level 1 & Level 2
- Phase 3: Level 3 & Level 4
- Phase 4: Level 5 & Level 5
- Phase 5: Level 7 & Level 8


## EOF (2022/08/30)