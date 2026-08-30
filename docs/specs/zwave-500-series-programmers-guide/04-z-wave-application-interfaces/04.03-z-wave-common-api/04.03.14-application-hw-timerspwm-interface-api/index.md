<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.14 Application HW Timers/PWM Interface API"
  pages: 244-272
-->
# 4.3.14 Application HW Timers/PWM Interface API

The 500 Series Z-Wave SoC has three built-in HW timers available for the application:

1. Timer0 2. Timer1 3. GPTimer or PWM generator.

| Timer | bits | Clocked by | Count up/down |
| --- | --- | --- | --- |
| Timer0 | 8/13/16 | 32MHz / 2 or P3.4 | Counts up |
| Timer1 | 8/13/16 | 32MHz / 2 or P3.5 | Counts up |
| GPTimer | 16 | 32MHz / 8 or 32MHz / 1024 | Counts down |

Timer0 and Timer1 are standard 8051 timers that can be configured to:

 be enabled/disabled  use the system clock divided by 2 (16MHz) or use a pin as clock source  generate an interrupt at overflow

Refer to figure below for principle diagrams of how the clock control works for Timer0.

Figure 17. Principle of Clock Control for Timer0

Refer to figure below for principle diagrams of how the clock control works for Timer0.

Figure 18. Principle of Clock Control (mode 0-2) for Timer1

Timer0 and Timer1 can operate in four different modes. Refer to the description of ZW_TIMER1_init

## Contents

- [4.3.14.1 ZW_TIMER0_init](04.03.14.01-zw_timer0_init.md)
- [4.3.14.2 ZW_TIMER1_init](04.03.14.02-zw_timer1_init.md)
- [4.3.14.3 ZW_TIMER0_INT_CLEAR / ZW_TIMER1_INT_CLEAR](04.03.14.03-zw_timer0_int_clear-zw_timer1_int_clear.md)
- [4.3.14.4 ZW_TIMER0_INT_ENABLE / ZW_TIMER1_INT_ENABLE](04.03.14.04-zw_timer0_int_enable-zw_timer1_int_enable.md)
- [4.3.14.5 ZW_TIMER0_ENABLE / ZW_TIMER1_ENABLE](04.03.14.05-zw_timer0_enable-zw_timer1_enable.md)
- [4.3.14.6 ZW_TIMER0_ext_clk / ZW_TIMER1_ext_clk](04.03.14.06-zw_timer0_ext_clk-zw_timer1_ext_clk.md)
- [4.3.14.7 ZW_TIMER0_LOWBYTE_SET / ZW_TIMER1_LOWBYTE_SET](04.03.14.07-zw_timer0_lowbyte_set-zw_timer1_lowbyte_set.md)
- [4.3.14.8 ZW_TIMER0_HIGHBYTE_SET / ZW_TIMER1_HIGHBYTE_SET](04.03.14.08-zw_timer0_highbyte_set-zw_timer1_highbyte_set.md)
- [4.3.14.9 ZW_TIMER0_HIGHBYTE_GET / ZW_TIMER1_HIGHBYTE_GET](04.03.14.09-zw_timer0_highbyte_get-zw_timer1_highbyte_get.md)
- [4.3.14.10 ZW_TIMER0_LOWBYTE_GET / ZW_TIMER1_LOWBYTE_GET](04.03.14.10-zw_timer0_lowbyte_get-zw_timer1_lowbyte_get.md)
- [4.3.14.11 ZW_TIMER0_word_get / ZW_TIMER1_word_get](04.03.14.11-zw_timer0_word_get-zw_timer1_word_get.md)
- [4.3.14.12 ZW_GPTIMER_init](04.03.14.12-zw_gptimer_init.md)
- [4.3.14.13 ZW_GPTIMER_int_clear](04.03.14.13-zw_gptimer_int_clear.md)
- [4.3.14.14 ZW_GPTIMER_int_get](04.03.14.14-zw_gptimer_int_get.md)
- [4.3.14.15 ZW_GPTIMER_int_enable](04.03.14.15-zw_gptimer_int_enable.md)
- [4.3.14.16 ZW_GPTIMER_enable](04.03.14.16-zw_gptimer_enable.md)
- [4.3.14.17 ZW_GPTIMER_pause](04.03.14.17-zw_gptimer_pause.md)
- [4.3.14.18 ZW_GPTIMER_reload_set](04.03.14.18-zw_gptimer_reload_set.md)
- [4.3.14.19 ZW_GPTIMER_reload_get](04.03.14.19-zw_gptimer_reload_get.md)
- [4.3.14.20 ZW_GPTIMER_get](04.03.14.20-zw_gptimer_get.md)
- [4.3.14.21 ZW_PWM_init](04.03.14.21-zw_pwm_init.md)
- [4.3.14.22 ZW_PWM_enable](04.03.14.22-zw_pwm_enable.md)
- [4.3.14.23 ZW_PWM_int_clear](04.03.14.23-zw_pwm_int_clear.md)
- [4.3.14.24 ZW_PWM_int_get](04.03.14.24-zw_pwm_int_get.md)
- [4.3.14.25 ZW_PWM_int_enable](04.03.14.25-zw_pwm_int_enable.md)
- [4.3.14.26 ZW_PWM_waveform_set](04.03.14.26-zw_pwm_waveform_set.md)
- [4.3.14.27 ZW_PWM_waveform_get](04.03.14.27-zw_pwm_waveform_get.md)
