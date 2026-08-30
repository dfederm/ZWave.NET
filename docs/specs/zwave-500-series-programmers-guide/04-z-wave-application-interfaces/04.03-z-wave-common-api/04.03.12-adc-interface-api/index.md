<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.12 ADC Interface API"
  pages: 206-225
-->
# 4.3.12 ADC Interface API

The ADC interface API provides access to an 8/12-bit ADC with input multiplexer.

[Refer to [16] for a detailed description of the ADC hardware.](../../../08-references.md#8-references)

[The ADC MAY be used for monitoring battery levels [15], voltages across various sensors etc. The ADC](../../../08-references.md#8-references) MAY be configured to generate an interrupt request if the measured voltage is above, below or equal to a threshold depending on the configuration settings. The ADC MAY use up to 4 GPIO as inputs depending on its configuration. Input pins that are not enabled MAY be used as GPIO's for by other peripherals.

Three sources can work as voltage-references for the ADC, namely either the power-supply for the chip, an internal 1.2V voltage-reference or the P3.7 pin (ADC_PIN3). The maximum sample rate when in continuous conversion mode is 23.6k sample/s for 8 bit conversions and 10.9k sample/s for 12 bit conversions.

The figures below show when the ADC interrupt is released dependent on, how the ADC threshold gradient is set:

| Tim |  |  |  |  |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |

Figure 10. Threshold Functionality when Threshold Gradient Set to High

| Tim |  |  |  |  |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |

Figure 11. Threshold Functionality when Threshold Gradient Set to Low The figure below shows how the connections to the ADC can be configured:

ADC

Comparator P3.7 Out P3.6 BG P3.5 P3.4 Buf.

Figure 12. Configuration of Input Pins

/************************************************* * To be placed in interrupt routine module **************************************************/

void adc_int (void) interrupt INUM_ADC { _push_(SFRPAGE); ZW_ADC_int_clear(); adc_triggered=TRUE; adc_value=ZW_ADC_result_get(); _pop_(SFRPAGE); }

:

/************************************************* * To be placed in applicationInitHW() **************************************************/

// Power up ADC and set ADC conversion mode, references, pins ZW_ADC_init(ADC_IO_MULTI_MODE,ADC_REF_U_VDD,ADC_REF_L_VSS,\ ADC_PIN1|ADC_PIN2); // Set auto zero period ZW_ADC_auto_zero_set(ADC_AZPL_128); // Set ADC resolution ZW_ADC_resolution_set(ADC_8_BIT); // Clear ADC interrupt flag ZW_ADC_int_clear(); // Enable ADC interrupt ZW_ADC_int_enable(TRUE);

: /************************************************* * To be placed in applicationPoll() **************************************************/

if (state==powerUp) { // select ADC input pin if (measure==sensor1) { // sensor 1 is on ADC pin 1 ZW_ADC_pin_select(ADC_PIN1); // enable lower threshold ZW_ADC_threshold_mode_set(ADC_THRES_LOWER); // set threshold level to ~25% of VDD ZW_ADC_threshold_set(0x0040); } else { // sensor 2 is on ADC pin 2 ZW_ADC_pin_select(ADC_PIN2); // enable upper threshold ZW_ADC_threshold_mode_set(ADC_THRES_UPPER); // set threshold level to ~50% of VDD ZW_ADC_threshold_set(0x0080); } // Start ADC ZW_ADC_enable(TRUE); state=xxx; }: if (state==running) { // React on sampled ADC value if (adc_triggered) { if (measure==sensor1) do_something1(adc_value); else do_something2(adc_value); adc_triggered=FALSE; }: }

Figure 13, ADC Code Sample Snippets Using an I/O as Input /************************************************* * To be placed in applicationInitHW() **************************************************/

// Set ADC to battery monitoring mode, other parameters are ignored ZW_ADC_init(ADC_BATT_SINGLE_MODE, 0, 0, 0); // Set auto zero period ZW_ADC_auto_zero_set(ADC_AZPL_128); // Set ADC resolution ZW_ADC_resolution_set(ADC_12_BIT);

/************************************************* * To be placed in applicationPoll() **************************************************/

if (state==startBatteryVoltageMeasurement) { // Power up ADC ZW_ADC_power_enable(TRUE); // Start ADC ZW_ADC_enable(TRUE); state=awatingBatteryVoltageMeasurement; }

:

if ((state==awatingBatteryVoltageMeasurement) { battLevel=ZW_ADC_result_get(); if (battLevel!= ADC_NOT_FINISHED)) { // Calc battery level in mV (Vbg is the band gab voltage) battVol=(DWORD)Vbg*4096/(DWORD)battLevel // Return battery voltage in mV send(battVol); state=xxx; // Power down ADC ZW_ADC_power_enable(FALSE); } } Figure 14, ADC Code Sample Snippets Using Battery Monitoring Mode

## Contents

- [4.3.12.1 ZW_ADC_init](04.03.12.01-zw_adc_init.md)
- [4.3.12.2 ZW_ADC_power_enable](04.03.12.02-zw_adc_power_enable.md)
- [4.3.12.3 ZW_ADC_enable](04.03.12.03-zw_adc_enable.md)
- [4.3.12.4 ZW_ADC_pin_select](04.03.12.04-zw_adc_pin_select.md)
- [4.3.12.5 ZW_ADC_threshold_mode_set](04.03.12.05-zw_adc_threshold_mode_set.md)
- [4.3.12.6 ZW_ADC_threshold_set](04.03.12.06-zw_adc_threshold_set.md)
- [4.3.12.7 ZW_ADC_int_enable](04.03.12.07-zw_adc_int_enable.md)
- [4.3.12.8 ZW_ADC_int_clear](04.03.12.08-zw_adc_int_clear.md)
- [4.3.12.9 ZW_ADC_is_fired](04.03.12.09-zw_adc_is_fired.md)
- [4.3.12.10 ZW_ADC_result_get](04.03.12.10-zw_adc_result_get.md)
- [4.3.12.11 ZW_ADC_buffer_enable](04.03.12.11-zw_adc_buffer_enable.md)
- [4.3.12.12 ZW_ADC_auto_zero_set](04.03.12.12-zw_adc_auto_zero_set.md)
- [4.3.12.13 ZW_ADC_resolution_set](04.03.12.13-zw_adc_resolution_set.md)
