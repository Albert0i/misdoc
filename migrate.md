# Migration Guide

## I. Target servers
- prod-rw: Read/Write 
- prod-ro: Read only 

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
- Extensive : change ot source code 
- Verbatim: Line-by-line re-write from one language to another

## III. Action plan
. . . 

## EOF (2022/08/30)