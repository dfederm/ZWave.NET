<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.11 SPI Interface API"
  pages: 186-206
-->
# 4.3.11 SPI Interface API

The 500 Series Z-Wave SoC offers up to two SPI interfaces:

SPI0: operate as a SPI master or as a SPI slave SPI1: operates as a SPI master

The SPI master, SPI1, is reserved by the Z-Wave protocol, if the 500 Series Z-Wave SoC is programmed as one of the following Z-Wave nodes types: Portable Controller, Static Controller, Bridge Controller, or Enhanced 232 Slave.

The state of the IO's used for SCK, MOSI, MISO and SS_N automatically setup by the SPI once it is enabled.

The SS_N input is used as SPI Slave Select for an SPI setup as a slave. If the SPI controller is master and it needs to select the slave(s), this has to be controlled by the application SW and an extra IO pin(s) has to be used for that purpose.

## Contents

- [4.3.11.1 Operation](04.03.11.01-operation.md)
- [4.3.11.2 ZW_SPI0_init](04.03.11.02-zw_spi0_init.md)
- [4.3.11.3 ZW_SPI0_enable](04.03.11.03-zw_spi0_enable.md)
- [4.3.11.4 ZW_SPI0_rx_get](04.03.11.04-zw_spi0_rx_get.md)
- [4.3.11.5 ZW_SPI0_tx_set](04.03.11.05-zw_spi0_tx_set.md)
- [4.3.11.6 ZW_SPI0_active_get](04.03.11.06-zw_spi0_active_get.md)
- [4.3.11.7 ZW_SPI0_coll_get](04.03.11.07-zw_spi0_coll_get.md)
- [4.3.11.8 ZW_SPI0_int_enable](04.03.11.08-zw_spi0_int_enable.md)
- [4.3.11.9 ZW_SPI0_int_get](04.03.11.09-zw_spi0_int_get.md)
- [4.3.11.10 ZW_SPI0_int_clear](04.03.11.10-zw_spi0_int_clear.md)
- [4.3.11.11 ZW_SPI1_init](04.03.11.11-zw_spi1_init.md)
- [4.3.11.12 ZW_SPI1_enable](04.03.11.12-zw_spi1_enable.md)
- [4.3.11.13 ZW_SPI1_rx_get](04.03.11.13-zw_spi1_rx_get.md)
- [4.3.11.14 ZW_SPI1_tx_set](04.03.11.14-zw_spi1_tx_set.md)
- [4.3.11.15 ZW_SPI1_active_get](04.03.11.15-zw_spi1_active_get.md)
- [4.3.11.16 ZW_SPI1_coll_get](04.03.11.16-zw_spi1_coll_get.md)
- [4.3.11.17 ZW_SPI1_int_enable](04.03.11.17-zw_spi1_int_enable.md)
- [4.3.11.18 ZW_SPI1_int_get](04.03.11.18-zw_spi1_int_get.md)
- [4.3.11.19 ZW_SPI1_int_clear](04.03.11.19-zw_spi1_int_clear.md)
