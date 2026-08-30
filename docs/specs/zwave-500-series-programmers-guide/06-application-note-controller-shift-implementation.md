<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "6 Application Note: Controller Shift Implementation"
  pages: 444-445
-->
# 6 Application Note: Controller Shift Implementation

This note describes how a controller is able to include a new controller that after the inclusion (add) will become the primary controller in the network. The controller that is taking over the primary functionality should just enter learn mode like when it is to be included in a network. The existing primary controller makes the controller change by calling ZW_ControllerChange (CONTROLLER_CHANGE_START,..). )

After a successfull change, the controller that called ZW_ControllerChange will be secondary and no longer able to include devices.

Figure 47. Controller Shift Frame Flow
