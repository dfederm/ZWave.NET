<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: SDS13783 Z-Wave Transport-Encapsulation Command Class Specification.pdf
  section: "3.6.5 Message Encapsulation"
  pages: 67-87
-->
# 3.6.5 Message Encapsulation

The Security 2 Command Class supports Singlecast as well as Multicast communication

The S2 Transport Layer MUST NOT provide retransmission if the security layer discards a message due to SPAN synchronization failure or failed authentication.

An application SHOULD use the Supervision Command Class for delivery acknowledgement of Security 2 Encapsulated commands. The Supervision Report command returns high-level status information on the execution status of the transmitted command which SHOULD be used by a controlling application instead of polling the destination node repeatedly.

## Contents

- [3.6.5.1 Singlecast messages and SPAN Management](03.06.05.01-singlecast-messages-and-span-management.md)
- [3.6.5.2 Multicast messages and MPAN Management](03.06.05.02-multicast-messages-and-mpan-management.md)
- [3.6.5.3 Message encapsulation commands](03.06.05.03-message-encapsulation-commands.md)
- [3.6.5.4 Duplicate Message Detection](03.06.05.04-duplicate-message-detection.md)
