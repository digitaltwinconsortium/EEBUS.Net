namespace EEBUS.Enums
{
    enum SHIPMessageType
    {
        INIT = 0,
        CONTROL,
        DATA,
        END
    }

    enum SHIPMessageTimeout
    {
        CMI_TIMEOUT = 30000 // maximum allowed according to spec
    }

    enum SHIPMessageValue
    {
        CMI_HEAD = 0
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