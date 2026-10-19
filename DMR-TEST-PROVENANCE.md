# DMR test-vector provenance

The 32 numeric bursts in src/DmrVectors.tsv were generated locally on October 17, 2026 from publicly available MMDVMHost Golay, BPTC and RS encoders, covering color codes 0–15 and full-link-control header/terminator types. They are test inputs, not production encoder code. Production decoder equations and matrices follow ETSI TS 102 361-1 v2.6.1.

References:
- https://www.etsi.org/deliver/etsi_ts/102300_102399/10236101/02.06.01_60/ts_10236101v020601p.pdf
- https://github.com/g4klx/MMDVMHost

Reference sources were downloaded for local comparison only and are not linked into the executable or bundled as application dependencies. Downloaded source hashes:
1CAE4C93E00644664BFCCD4F695269F67D347815B9A88FA3CB8EDB147A32E961  Golay2087.cpp
EBC9C88EFDA5C13148A91A8B9AE40DE006356EC32BFB1979BF943474C93BEE96  BPTC19696.cpp
3CB7F8315E9583A3AB28FAB2D5947808E3A282020E2C92EF1DF7A8371AEA11CF  RS129.cpp
