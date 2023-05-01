namespace DungeonDiscordBot.Utilities;

// **********************************************
// *** Synchronized access wrapper class V1.0 ***
// **********************************************
// *** (C)2009 S.T.A. snc                     ***
// **********************************************

internal class Synchronized<T>
{
    // *** Locking ***
    private readonly object m_ValueLock;

    // *** Value buffer ***
    private readonly T m_Value = default!;

    // *** Access to value ***
    private T Value
    {
        get
        {
            lock (m_ValueLock)
            {
                return m_Value;
            }
        }
        init
        {
            lock (m_ValueLock)
            {
                m_Value = value;
            }
        }
    }

    // *******************
    // *** Constructor ***
    // *******************
    internal Synchronized(T value)
    {
        m_ValueLock = new object();
        Value = value;
    }

    internal Synchronized(T value, object Lock)
    {
        m_ValueLock = Lock;
        Value = value;
    }

    // ********************************
    // *** Type casting overloading ***
    // ********************************
    public static implicit operator T(Synchronized<T> value)
    {
        return value.Value;
    }

}