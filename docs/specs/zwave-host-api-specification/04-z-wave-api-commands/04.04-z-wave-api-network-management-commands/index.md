<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: Z-Wave Host API Specification.pdf
  section: "4.4 Z-Wave API Network Management Commands"
  pages: 95-193
-->
# 4.4 Z-Wave API Network Management Commands

[This section describes Z-Wave API Commands that are used to perform Z-Wave Network Management.](../index.md#4-z-wave-api-commands)

## Contents

- [4.4.1 Common Network Management Commands](04.04.01-common-network-management-commands.md)
- [4.4.1.1 Send NOP Command](04.04.01.01-send-nop-command.md)
- [4.4.1.2 Get Node Information Protocol Data Command](04.04.01.02-get-node-information-protocol-data-command.md)
- [4.4.1.3 Send Node Information Command](04.04.01.03-send-node-information-command.md)
- [4.4.1.4 Request Node Information Command](04.04.01.04-request-node-information-command.md)
- [4.4.1.5 Set Learn Mode Command](04.04.01.05-set-learn-mode-command.md)
- [4.4.1.6 Get SUC NodeID Command](04.04.01.06-get-suc-nodeid-command.md)
- [4.4.1.7 Set SmartStart Inclusion Request Maximum Interval Command](04.04.01.07-set-smartstart-inclusion-request-maximum-interval-command.md)
- [4.4.1.8 Explore Request Inclusion Command](04.04.01.08-explore-request-inclusion-command.md)
- [4.4.1.9 Explore Request Exclusion Command](04.04.01.09-explore-request-exclusion-command.md)
- [4.4.2 End Nodes Network Management](04.04.02-end-nodes-network-management.md)
- [4.4.2.1 Request New Route Destinations Command](04.04.02.01-request-new-route-destinations-command.md)
- [4.4.2.2 Is Node Within Direct Range Command](04.04.02.02-is-node-within-direct-range-command.md)
- [4.4.2.3 Get Network Statistics Command](04.04.02.03-get-network-statistics-command.md)
- [4.4.2.4 Clear Network Statistics Command](04.04.02.04-clear-network-statistics-command.md)
- [4.4.3 Controller Nodes Network Management](04.04.03-controller-nodes-network-management.md)
- [4.4.3.1 Add Node To Network Command](04.04.03.01-add-node-to-network-command.md)
- [4.4.3.2 Add Controller And Assign Primary Controller Role Command](04.04.03.02-add-controller-and-assign-primary-controller-role-command.md)
- [4.4.3.3 Add Primary Controller Command](04.04.03.03-add-primary-controller-command.md)
- [4.4.3.4 Remove Node From Network Command](04.04.03.04-remove-node-from-network-command.md)
- [4.4.3.5 Remove Specific Node From Network Command](04.04.03.05-remove-specific-node-from-network-command.md)
- [4.4.3.6 Is Node Failed Command](04.04.03.06-is-node-failed-command.md)
- [4.4.3.7 Remove Failed Node Command](04.04.03.07-remove-failed-node-command.md)
- [4.4.3.8 Replace Failed Node Command](04.04.03.08-replace-failed-node-command.md)
- [4.4.3.9 Delete Return Route Command](04.04.03.09-delete-return-route-command.md)
- [4.4.3.10 Assign Return Route Command](04.04.03.10-assign-return-route-command.md)
- [4.4.3.11 Assign SUC Return Route Command](04.04.03.11-assign-suc-return-route-command.md)
- [4.4.3.12 Assign Priority Return Route Command](04.04.03.12-assign-priority-return-route-command.md)
- [4.4.3.13 Assign Priority SUC Return Route Command](04.04.03.13-assign-priority-suc-return-route-command.md)
- [4.4.3.14 Set Priority Route Command](04.04.03.14-set-priority-route-command.md)
- [4.4.3.15 Get Priority Route Command](04.04.03.15-get-priority-route-command.md)
- [4.4.3.16 Lock Unlock Last Route Command](04.04.03.16-lock-unlock-last-route-command.md)
- [4.4.3.17 Set SUC NodeID Command](04.04.03.17-set-suc-nodeid-command.md)
- [4.4.3.18 Delete SUC Return Route Command](04.04.03.18-delete-suc-return-route-command.md)
- [4.4.3.19 Send SUC NodeID Command](04.04.03.19-send-suc-nodeid-command.md)
- [4.4.3.20 Request Node Neighbor Discovery Command](04.04.03.20-request-node-neighbor-discovery-command.md)
- [4.4.3.21 Request Network Update Command](04.04.03.21-request-network-update-command.md)
- [4.4.3.22 Set Virtual Node To Learn Mode Command](04.04.03.22-set-virtual-node-to-learn-mode-command.md)
- [4.4.3.23 Virtual Node Send Node Information Command](04.04.03.23-virtual-node-send-node-information-command.md)
- [4.4.3.24 Set Virtual Nodes Application Node Information Command](04.04.03.24-set-virtual-nodes-application-node-information-command.md)
- [4.4.3.25 Set Z-Wave Long Range Shadow NodeIDs Commmand](04.04.03.25-set-z-wave-long-range-shadow-nodeids-commmand.md)
