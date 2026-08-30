<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.4 Z-Wave Controller API"
  pages: 348-412
-->
# 4.4 Z-Wave Controller API

The Z-Wave Controller API makes it possible for different controllers to control the Z-Wave nodes and get information about each node’s capabilities and current state. The node control commands can be sent to a single node, all nodes or to a list of nodes (group, scene…).

## Contents

- [4.4.1 ZW_AddNodeToNetwork](04.04.01-zw_addnodetonetwork.md)
- [4.4.2 ZW_AddNodeDskToNetwork](04.04.02-zw_addnodedsktonetwork.md)
- [4.4.3 ZW_AreNodesNeighbours](04.04.03-zw_arenodesneighbours.md)
- [4.4.4 ZW_AssignReturnRoute](04.04.04-zw_assignreturnroute.md)
- [4.4.5 ZW_AssignSUCReturnRoute](04.04.05-zw_assignsucreturnroute.md)
- [4.4.6 ZW_AssignPriorityReturnRoute](04.04.06-zw_assignpriorityreturnroute.md)
- [4.4.7 ZW_AssignPrioritySUCReturnRoute](04.04.07-zw_assignprioritysucreturnroute.md)
- [4.4.8 ZW_ControllerChange](04.04.08-zw_controllerchange.md)
- [4.4.9 ZW_DeleteReturnRoute](04.04.09-zw_deletereturnroute.md)
- [4.4.10 ZW_DeleteSUCReturnRoute](04.04.10-zw_deletesucreturnroute.md)
- [4.4.11 ZW_GetControllerCapabilities](04.04.11-zw_getcontrollercapabilities.md)
- [4.4.12 ZW_GetNeighborCount](04.04.12-zw_getneighborcount.md)
- [4.4.13 ZW_GetPriorityRoute](04.04.13-zw_getpriorityroute.md)
- [4.4.14 ZW_SetPriorityRoute](04.04.14-zw_setpriorityroute.md)
- [4.4.15 ZW_GetNodeProtocolInfo](04.04.15-zw_getnodeprotocolinfo.md)
- [4.4.16 ZW_GetRoutingInfo](04.04.16-zw_getroutinginfo.md)
- [4.4.17 ZW_GetSUCNodeID](04.04.17-zw_getsucnodeid.md)
- [4.4.18 ZW_IsFailedNode](04.04.18-zw_isfailednode.md)
- [4.4.19 ZW_IsPrimaryCtrl](04.04.19-zw_isprimaryctrl.md)
- [4.4.20 ZW_RemoveFailedNode](04.04.20-zw_removefailednode.md)
- [4.4.21 ZW_ReplaceFailedNode](04.04.21-zw_replacefailednode.md)
- [4.4.22 ZW_RemoveNodeFromNetwork](04.04.22-zw_removenodefromnetwork.md)
- [4.4.23 ZW_RemoveNodeIDFromNetwork](04.04.23-zw_removenodeidfromnetwork.md)
- [4.4.24 ZW_ReplicationReceiveComplete](04.04.24-zw_replicationreceivecomplete.md)
- [4.4.25 ZW_ReplicationSend](04.04.25-zw_replicationsend.md)
- [4.4.26 ZW_RequestNodeInfo](04.04.26-zw_requestnodeinfo.md)
- [4.4.27 ZW_RequestNodeNeighborUpdate](04.04.27-zw_requestnodeneighborupdate.md)
- [4.4.28 ZW_SendSUCID](04.04.28-zw_sendsucid.md)
- [4.4.29 ZW_SetDefault](04.04.29-zw_setdefault.md)
- [4.4.30 ZW_SetLearnMode](04.04.30-zw_setlearnmode.md)
- [4.4.31 ZW_SetRoutingInfo](04.04.31-zw_setroutinginfo.md)
- [4.4.32 ZW_SetRoutingMAX](04.04.32-zw_setroutingmax.md)
- [4.4.33 ZW_SetSUCNodeID](04.04.33-zw_setsucnodeid.md)
