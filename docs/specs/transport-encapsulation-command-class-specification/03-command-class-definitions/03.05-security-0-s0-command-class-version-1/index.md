<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: SDS13783 Z-Wave Transport-Encapsulation Command Class Specification.pdf
  section: "3.5 Security 0 (S0) Command Class, version 1"
  pages: 34-52
-->
# 3.5 Security 0 (S0) Command Class, version 1

The Security Command Class create the foundation for secure application communication between nodes in a Z-Wave network. The security layer provides confidentiality, authentication and replay attack robustness through AES-128.

The Security Command Class defines a number of commands used to facilitate handling of encrypted frames in a Z-Wave Network. The commands deal with three main areas:

 Message Encapsulation. The task of taking a plain text frame and encapsulating the frame into an encrypted Security Message.

 Command Class Handling. The task of handling what command classes are supported when communicating with a Security enabled device

 Network Key Management. The task of initial key distribution.

Compatibility considerations

A node supporting the S0 Command Class MAY use the S2 CTR_DRBG as a PNRG.

## Contents

- [3.5.1.1 Node Information Frame (NIF)](03.05.01.01-node-information-frame-nif.md)
- [3.5.2 Message Encapsulation and Command Class Handling](03.05.02-message-encapsulation-and-command-class-handling.md)
- [3.5.3 Network Key Management](03.05.03-network-key-management.md)
- [3.5.4 Encapsulated Command Class Handling](03.05.04-encapsulated-command-class-handling.md)
