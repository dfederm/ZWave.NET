<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: Z-Wave Host API Specification.pdf
  section: "4.9 Z-Wave API Transport Commands"
  pages: 237-257
-->
# 4.9 Z-Wave API Transport Commands

[This section describes Z-Wave API Commands that are used to perform transport operations.](../index.md#4-z-wave-api-commands)

## Contents

- [4.9.1 Controller Node Send Data Command](04.09.01-controller-node-send-data-command.md)
- [4.9.1.1 Frame flow](04.09.01.01-frame-flow.md)
- [4.9.1.2 1. Initial data frame (host → Z-Wave Module)](04.09.01.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.1.3 2. Response data frame (Z-Wave Module → host)](04.09.01.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.1.4 3. Callback data frame (Z-Wave Module → host)](04.09.01.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.2 Controller Node Send Data Multicast Command](04.09.02-controller-node-send-data-multicast-command.md)
- [4.9.2.1 Frame flow](04.09.02.01-frame-flow.md)
- [4.9.2.2 1. Initial data frame (host → Z-Wave Module)](04.09.02.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.2.3 2. Response data frame (Z-Wave Module → host)](04.09.02.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.2.4 3. Callback data frame (Z-Wave Module → host)](04.09.02.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.3 End Node Send Data Command](04.09.03-end-node-send-data-command.md)
- [4.9.3.1 Frame flow](04.09.03.01-frame-flow.md)
- [4.9.3.2 1. Initial data frame (host → Z-Wave Module)](04.09.03.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.3.3 2. Response data frame (Z-Wave Module → host)](04.09.03.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.3.4 3. Callback data frame (Z-Wave Module → host)](04.09.03.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.4 End Node Send Data Multicast Command](04.09.04-end-node-send-data-multicast-command.md)
- [4.9.4.1 Frame flow](04.09.04.01-frame-flow.md)
- [4.9.4.2 1. Initial data frame (host → Z-Wave Module)](04.09.04.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.4.3 2. Response data frame (Z-Wave Module → host)](04.09.04.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.4.4 3. Callback data frame (Z-Wave Module → host)](04.09.04.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.5 Bridge Controller Node Send Data Command](04.09.05-bridge-controller-node-send-data-command.md)
- [4.9.5.1 Frame flow](04.09.05.01-frame-flow.md)
- [4.9.5.2 1. Initial data frame (host → Z-Wave Module)](04.09.05.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.5.3 2. Response data frame (Z-Wave Module → host)](04.09.05.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.5.4 3. Callback data frame (Z-Wave Module → host)](04.09.05.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.6 Bridge Controller Node Send Data Multicast Command](04.09.06-bridge-controller-node-send-data-multicast-command.md)
- [4.9.6.1 Frame flow](04.09.06.01-frame-flow.md)
- [4.9.6.2 1. Initial data frame (host → Z-Wave Module)](04.09.06.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.6.3 2. Response data frame (Z-Wave Module → host)](04.09.06.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.6.4 3. Callback data frame (Z-Wave Module → host)](04.09.06.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.7 Send Data Abort Command](04.09.07-send-data-abort-command.md)
- [4.9.7.1 Frame flow](04.09.07.01-frame-flow.md)
- [4.9.7.2 1. Initial data frame (host → Z-Wave Module)](04.09.07.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.7.3 2. Response data frame (Z-Wave Module → host)](04.09.07.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.7.4 3. Callback data frame (Z-Wave Module → host)](04.09.07.04-3-callback-data-frame-z-wave-module-host.md)
- [4.9.8 Send Test Frame Command](04.09.08-send-test-frame-command.md)
- [4.9.8.1 Frame flow](04.09.08.01-frame-flow.md)
- [4.9.8.2 1. Initial data frame (host → Z-Wave Module)](04.09.08.02-1-initial-data-frame-host-z-wave-module.md)
- [4.9.8.3 2. Response data frame (Z-Wave Module → host)](04.09.08.03-2-response-data-frame-z-wave-module-host.md)
- [4.9.8.4 3. Callback data frame (Z-Wave Module → host)](04.09.08.04-3-callback-data-frame-z-wave-module-host.md)
