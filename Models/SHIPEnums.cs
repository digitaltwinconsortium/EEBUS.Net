namespace EEBUS.Enums
{
    public class SHIPMessageType
    {
        public const byte INIT = 0;
        public const byte CONTROL = 1;
        public const byte DATA = 2;
        public const byte END = 3;
    }

    public class SHIPMessageTimeout
    {
        public const int CMI_TIMEOUT = 30 * 1000; // maximum allowed are 30 seconds, according to spec
        public const int T_HELLO_INIT = 240 * 1000; // maximum alowed are 240 seconds, according to the spec
        public const int T_HELLO_INC = T_HELLO_INIT;
        public const int T_HELLO_PROLONG_THR_INC = 30 * 1000;// maximum allowed are 30 seconds, according to spec
        public const int T_HELLO_PROLONG_WAITING_GAP = 15 * 1000;// maximum allowed are 15 seconds, according to spec
        public const int T_HELLO_PROLONG_MIN = 1000;// maximum allowed is 1 second, according to spec
    }

    public class SHIPMessageValue
    {
        public const byte CMI_HEAD = 0;
    }

    enum SHIPConnectionModeInitialisation
    {
        CONNECTION_DATA_PREPARATION = 0
    }

    enum SHIPMessageExchangeStates
    {
        SME_HELLO_STATE_READY_INIT,
        SME_HELLO_STATE_READY_LISTEN,
        SME_HELLO_STATE_READY_TIMEOUT,
        SME_HELLO_STATE_PENDING_INIT,
        SME_HELLO_STATE_PENDING_LISTEN,
        SME_HELLO_STATE_PENDING_TIMEOUT
    }
}