<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.3 Z-Wave Transport API"
  pages: 101-137
-->
# 4.3.3 Z-Wave Transport API

The Z-Wave transport layer controls transfer of data between Z-Wave nodes including retransmission, frame check and acknowledgement. The Z-Wave transport interface includes functions for transfer of data to other Z-Wave nodes. Application data received from other nodes is handed over to the application via the ApplicationCommandHandler function. The ZW_MAX_NODES define defines the maximum of nodes possible in a Z-Wave network.

## Contents

- [4.3.3.1 ZW_SendData](04.03.03.01-zw_senddata.md)
- [4.3.3.2 ZW_SendDataEx (only Slave Libraries)](04.03.03.02-zw_senddataex-only-slave-libraries.md)
- [4.3.3.3 ZW_SendData_Bridge](04.03.03.03-zw_senddata_bridge.md)
- [4.3.3.4 ZW_SendDataMulti](04.03.03.04-zw_senddatamulti.md)
- [4.3.3.5 ZW_SendDataMultiEx (only Slave Libraries)](04.03.03.05-zw_senddatamultiex-only-slave-libraries.md)
- [4.3.3.6 ZW_SendDataMulti_Bridge](04.03.03.06-zw_senddatamulti_bridge.md)
- [4.3.3.7 ZW_SendDataAbort](04.03.03.07-zw_senddataabort.md)
- [4.3.3.8 ZW_LockRoute (only Controllers)](04.03.03.08-zw_lockroute-only-controllers.md)
- [4.3.3.9 ZW_LockRoute (only Slaves)](04.03.03.09-zw_lockroute-only-slaves.md)
- [4.3.3.10 ZW_SendConst](04.03.03.10-zw_sendconst.md)
- [4.3.3.11 ZW_SetListenBeforeTalkThreshold](04.03.03.11-zw_setlistenbeforetalkthreshold.md)
- [4.3.3.12 ZW_Transport_CommandClassVersionGet](04.03.03.12-zw_transport_commandclassversionget.md)
- [4.3.3.13 ZW_GetDefaultPowerLevels](04.03.03.13-zw_getdefaultpowerlevels.md)
- [4.3.3.14 ZW_SetDefaultPowerLevels](04.03.03.14-zw_setdefaultpowerlevels.md)
