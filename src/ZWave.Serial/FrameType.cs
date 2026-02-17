namespace ZWave.Serial;

public enum FrameType
{
    /// <summary>
    /// The ACK frame indicates that the receiving end received a valid Data frame.
    /// </summary>
    ACK,

    /// <summary>
    /// The NAK frame indicates that the receiving end received a Data frame with errors.
    /// </summary>
    NAK,

    /// <summary>
    /// The CAN frame indicates that the receiving end discarded an otherwise valid Data frame.
    /// The CAN frame is used to resolve race conditions, where both ends send a Data frame and
    /// subsequently expects an ACK frame from the other end.
    /// </summary>
    CAN,

    /// <summary>
    /// A data frame contains the Serial API command including parameters for the command in question.
    /// </summary>
    Data
}
