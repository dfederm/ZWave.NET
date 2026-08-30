<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.13 UART Interface API"
  pages: 225-244
-->
# 4.3.13 UART Interface API

The UART (Universal Asynchronous Receiver Transmitter) interface is for serial communication with external devices such as PC’s, host controllers etc. The two UART interfaces transmits data in an asynchronous way, and is a two-way communication protocol, using 2 pins each as a communications means: TxD and RxD. The two pins can be enabled and disabled individually. If only using RX mode the TxD pin can be used as general IO pins and vice versa. The UART’s use dedicated timers and do not take up any 8051 timer resources.

Since the two UART’s are identical the description of each function is collapsed using the notation UARTx, where x is either 0 or 1.

The UARTx supports full duplex and can operate with the baud rates between 9.6kbaud and 230.4 kbaud. (See under ZW_UARTx_init)

The interface operates with 8 bit words, one start bit (low), one stop bit (high) and no parity. This setup is hardwired and can not be changed.

The UARTx shifts data in/out in the following order: start bit, data bits (LSB first) and stop bit. The figure below gives the waveform of a serial byte.

| D0 | D1 | D2 | D3 | D4 | D5 | D6 | D7 |
| --- | --- | --- | --- | --- | --- | --- | --- |

Figure 15. Serial Waveform

## Contents

- [4.3.13.1 Transmission](04.03.13.01-transmission.md)
- [4.3.13.2 Reception](04.03.13.02-reception.md)
- [4.3.13.3 RS232](04.03.13.03-rs232.md)
- [4.3.13.4 Integration](04.03.13.04-integration.md)
- [4.3.13.5 Operation](04.03.13.05-operation.md)
- [4.3.13.6 ZW_UART0_init / ZW_UART1_init](04.03.13.06-zw_uart0_init-zw_uart1_init.md)
- [4.3.13.7 ZW_UART0_rx_data_get / ZW_UART1_rx_data_get](04.03.13.07-zw_uart0_rx_data_get-zw_uart1_rx_data_get.md)
- [4.3.13.8 ZW_UART0_rx_data_wait_get / ZW_UART1_rx_data_wait_get](04.03.13.08-zw_uart0_rx_data_wait_get-zw_uart1_rx_data_wait_get.md)
- [4.3.13.9 ZW_UART0_tx_active_get / ZW_UART1_tx_active_get](04.03.13.09-zw_uart0_tx_active_get-zw_uart1_tx_active_get.md)
- [4.3.13.10 ZW_UART0_tx_data_set / ZW_UART1_tx_data_set](04.03.13.10-zw_uart0_tx_data_set-zw_uart1_tx_data_set.md)
- [4.3.13.11 ZW_UART0_tx_send_num / ZW_UART1_tx_send_num](04.03.13.11-zw_uart0_tx_send_num-zw_uart1_tx_send_num.md)
- [4.3.13.12 ZW_UART0_tx_send_str / ZW_UART1_tx_send_str](04.03.13.12-zw_uart0_tx_send_str-zw_uart1_tx_send_str.md)
- [4.3.13.13 ZW_UART0_INT_ENABLE / ZW_UART1_INT_ENABLE](04.03.13.13-zw_uart0_int_enable-zw_uart1_int_enable.md)
- [4.3.13.14 ZW_UART0_INT_DISABLE / ZW_UART1_INT_DISABLE](04.03.13.14-zw_uart0_int_disable-zw_uart1_int_disable.md)
- [4.3.13.15 ZW_UART0_tx_send_nl / ZW_UART1_tx_send_nl](04.03.13.15-zw_uart0_tx_send_nl-zw_uart1_tx_send_nl.md)
- [4.3.13.16 ZW_UART0_tx_int_clear / ZW_UART1_tx_int_clear](04.03.13.16-zw_uart0_tx_int_clear-zw_uart1_tx_int_clear.md)
- [4.3.13.17 ZW_UART0_rx_int_clear / ZW_UART1_rx_int_clear](04.03.13.17-zw_uart0_rx_int_clear-zw_uart1_rx_int_clear.md)
- [4.3.13.18 ZW_UART0_tx_int_get / ZW_UART1_tx_int_get](04.03.13.18-zw_uart0_tx_int_get-zw_uart1_tx_int_get.md)
- [4.3.13.19 ZW_UART0_rx_int_get / ZW_UART1_rx_int_get](04.03.13.19-zw_uart0_rx_int_get-zw_uart1_rx_int_get.md)
- [4.3.13.20 ZW_UART0_rx_enable / ZW_UART1_rx_enable](04.03.13.20-zw_uart0_rx_enable-zw_uart1_rx_enable.md)
- [4.3.13.21 ZW_UART0_tx_enable / ZW_UART1_tx_enable](04.03.13.21-zw_uart0_tx_enable-zw_uart1_tx_enable.md)
