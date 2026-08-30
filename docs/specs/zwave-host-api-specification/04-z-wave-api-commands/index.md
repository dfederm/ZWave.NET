<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: Z-Wave Host API Specification.pdf
  section: "4 Z-Wave API Commands"
  pages: 36-263
-->
# 4 Z-Wave API Commands

This section lists all defined Z-Wave API commands. Note that all commands are not always supported by a Z-Wave API Module.

[The Command format details common features and fields shared among several commands.](04.01-command-format.md#41-command-format)

The subsequent sections are grouping the Z-Wave API Commands in categories.

• [Z-Wave Capability API commands :](04.03-z-wave-capability-api-commands/index.md#43-z-wave-capability-api-commands)

This subsection groups all the Z-Wave API commands to read the Z-Wave API Module capabili- ties and perform initialization and setup.

• [Z-Wave API Network Management Commands :](04.04-z-wave-api-network-management-commands/index.md#44-z-wave-api-network-management-commands)

This subsection groups all the Z-Wave API commands allowing to perform Network Management [operations. Most of these operations are defined in [ zwave_nwk_spec ] for details. It is split into](../05-references.md#5-references) 3 subsubsections:

– [Commands available for all nodes: Common Network Management Commands](04.04-z-wave-api-network-management-commands/04.04.01-common-network-management-commands.md#441-common-network-management-commands)

– [Commands for controller nodes only: Controller Nodes Network Management](04.04-z-wave-api-network-management-commands/04.04.03-controller-nodes-network-management.md#443-controller-nodes-network-management)

[–](04.09-z-wave-api-transport-commands/index.md#49-z-wave-api-transport-commands) [Commands for end nodes only: End Nodes Network Management](04.04-z-wave-api-network-management-commands/04.04.02-end-nodes-network-management.md#442-end-nodes-network-management)

• [Z-Wave API Transport Commands :](04.09-z-wave-api-transport-commands/index.md#49-z-wave-api-transport-commands)

This subsection groups all the Z-Wave API commands that can be used to transmit application payloads.

• [Z-Wave API Firmware Update Commands :](04.06-z-wave-api-firmware-update-commands.md#46-z-wave-api-firmware-update-commands)

This subsection groups all the Z-Wave API commands that can be used to read and write the firmware of the Z-Wave API module.

• [Z-Wave API Security Commands :](04.10-z-wave-api-security-commands.md#410-z-wave-api-security-commands)

This subsection groups all the Z-Wave API commands related to security functionalities provided by the Z-Wave API Module.

• [Z-Wave API Memory Commands :](04.05-z-wave-api-memory-commands.md#45-z-wave-api-memory-commands)

This subsection groups all the Z-Wave API commands that can be used to read data that has been saved by the Z-Wave API Module in its persistent memory.

• [Unsolicited Z-Wave API commands :](04.07-unsolicited-z-wave-api-commands.md#47-unsolicited-z-wave-api-commands)

[This subsection groups all the Z-Wave API commands that are sent as unsolicited frames (refer to Data Frame and](../03-interface-communication/03.02-frame-types.md#321-data-frame) [Unsolicited frame ) by the Z-Wave API Module.](../03-interface-communication/03.03-command-frame-flows.md#336-unsolicited-frame)

• [Z-Wave API Miscellaneous Commands :](04.08-z-wave-api-miscellaneous-commands/index.md#48-z-wave-api-miscellaneous-commands)

This subsection groups all the Z-Wave API commands that do not fit in any of the other categories.

## Contents

- [4.1 Command format](04.01-command-format.md)
- [4.2 Generic command elements](04.02-generic-command-elements.md)
- [4.3 Z-Wave Capability API commands](04.03-z-wave-capability-api-commands/index.md)
- [4.4 Z-Wave API Network Management Commands](04.04-z-wave-api-network-management-commands/index.md)
- [4.5 Z-Wave API Memory Commands](04.05-z-wave-api-memory-commands.md)
- [4.6 Z-Wave API Firmware Update Commands](04.06-z-wave-api-firmware-update-commands.md)
- [4.7 Unsolicited Z-Wave API commands](04.07-unsolicited-z-wave-api-commands.md)
- [4.8 Z-Wave API Miscellaneous Commands](04.08-z-wave-api-miscellaneous-commands/index.md)
- [4.9 Z-Wave API Transport Commands](04.09-z-wave-api-transport-commands/index.md)
- [4.10 Z-Wave API Security Commands](04.10-z-wave-api-security-commands.md)
