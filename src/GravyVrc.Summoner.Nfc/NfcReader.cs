using System.ComponentModel;

namespace GravyVrc.Summoner.Nfc;

/// <summary>
/// Stolen from <a href="https://github.com/h4kbas/nfc-reader">h4kbas/nfc-reader</a>
/// </summary>
internal class NfcReader : IDisposable, INfcReader
{
    public int retCode, hCard, Protocol;
    int hContext;
    public bool connActive = false;
    public byte[] SendBuffer = new byte[263];
    public byte[] ReceiveBuffer = new byte[263];
    public int SendLength, ReceiveLength;
    internal enum SmartcardState
    {
        None = 0,
        Inserted = 1,
        Ejected = 2
    }

    public delegate void CardEventHandler();
    public event CardEventHandler? CardInserted;
    public event CardEventHandler? CardEjected;
    public event CardEventHandler? DeviceDisconnected;
    private BackgroundWorker? _worker;
    private Card.SCARD_READERSTATE RdrState;
    private string readername;
    private Card.SCARD_READERSTATE[]? states;
    private void WaitChangeStatus(object sender, DoWorkEventArgs e)
    {
        while (!e.Cancel)
        {
            int nErrCode = Card.SCardGetStatusChange(hContext, 1000, ref states[0], 1);

            if (nErrCode == Card.SCARD_E_SERVICE_STOPPED)
            {
                DeviceDisconnected();
                e.Cancel = true;
            }

            //Check if the state changed from the last time.
            if ((states[0].RdrEventState & 2) == 2)
            {
                //Check what changed.
                SmartcardState state = SmartcardState.None;
                if ((states[0].RdrEventState & 32) == 32 && (states[0].RdrCurrState & 32) != 32)
                {
                    //The card was inserted. 
                    state = SmartcardState.Inserted;
                }
                else if ((states[0].RdrEventState & 16) == 16 && (states[0].RdrCurrState & 16) != 16)
                {
                    //The card was ejected.
                    state = SmartcardState.Ejected;
                }
                if (state != SmartcardState.None && states[0].RdrCurrState != 0)
                {
                    switch (state)
                    {
                        case SmartcardState.Inserted:
                            {
                                //MessageBox.Show("Card inserted");
                                CardInserted();
                                break;
                            }
                        case SmartcardState.Ejected:
                            {
                                //MessageBox.Show("Card ejected");
                                CardEjected();
                                break;
                            }
                        default:
                            {
                                //MessageBox.Show("Some other state...");
                                break;
                            }
                    }
                }
                //Update the current state for the next time they are checked.
                states[0].RdrCurrState = states[0].RdrEventState;
            }
        }
    }
    internal Card.SCARD_IO_REQUEST pioSendRequest;
    private int SendAPDUandDisplay(int reqType)
    {
        int indx;
        string tmpStr = "";

        pioSendRequest.dwProtocol = Protocol;
        pioSendRequest.cbPciLength = 8;

        //Display Apdu In
        for (indx = 0; indx <= SendLength - 1; indx++)
        {
            tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuffer[indx]);
        }

        retCode = Card.SCardTransmit(hCard, ref pioSendRequest, ref SendBuffer[0],
                             SendLength, ref pioSendRequest, ref ReceiveBuffer[0], ref ReceiveLength);

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            return retCode;
        }

        else
        {
            try
            {
                tmpStr = "";
                switch (reqType)
                {
                    case 0:
                        for (indx = (ReceiveLength - 2); indx <= (ReceiveLength - 1); indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", ReceiveBuffer[indx]);
                        }

                        if ((tmpStr).Trim() != "90 00")
                        {
                            //MessageBox.Show("Return bytes are not acceptable.");
                            return -202;
                        }

                        break;

                    case 1:

                        for (indx = (ReceiveLength - 2); indx <= (ReceiveLength - 1); indx++)
                        {
                            tmpStr = tmpStr + string.Format("{0:X2}", ReceiveBuffer[indx]);
                        }

                        if (tmpStr.Trim() != "90 00")
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", ReceiveBuffer[indx]);
                        }

                        else
                        {
                            tmpStr = "ATR : ";
                            for (indx = 0; indx <= (ReceiveLength - 3); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", ReceiveBuffer[indx]);
                            }
                        }

                        break;

                    case 2:

                        for (indx = 0; indx <= (ReceiveLength - 1); indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", ReceiveBuffer[indx]);
                        }

                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return -200;
            }
        }
        return retCode;
    }

    private void ClearBuffers()
    {
        long indx;

        for (indx = 0; indx <= 262; indx++)
        {
            ReceiveBuffer[indx] = 0;
            SendBuffer[indx] = 0;
        }
    }

    private bool AuthBlock(byte block)
    {
        ClearBuffers();
        SendBuffer[0] = 0xFF;                         // CLA
        SendBuffer[2] = 0x00;                         // P1: same for all source types 
        SendBuffer[1] = 0x86;                         // INS: for stored key input
        SendBuffer[3] = 0x00;                         // P2 : Memory location;  P2: for stored key input
        SendBuffer[4] = 0x05;                         // P3: for stored key input
        SendBuffer[5] = 0x01;                         // Byte 1: version number
        SendBuffer[6] = 0x00;                         // Byte 2
        SendBuffer[7] = block;       // Byte 3: sectore no. for stored key input
        SendBuffer[8] = 0x61;                         // Byte 4 : Key B for stored key input
        SendBuffer[9] = 1;         // Byte 5 : Session key for non-volatile memory

        SendLength = 0x0A;
        ReceiveLength = 0x02;

        retCode = SendAPDUandDisplay(0);

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            //MessageBox.Show("FAIL Authentication! No:" + retCode.ToString());
            return false;
        }

        return true;
    }

    public string GetCardUID()
    {
        byte[] receivedUID = new byte[256];
        var request = new Card.SCARD_IO_REQUEST
        {
            dwProtocol = Card.SCARD_PROTOCOL_T1,
            cbPciLength = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Card.SCARD_IO_REQUEST))
        };
        byte[] sendBytes = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
        int outBytes = receivedUID.Length;
        int status = Card.SCardTransmit(hCard, ref request, ref sendBytes[0], sendBytes.Length, ref request, ref receivedUID[0], ref outBytes);

        string cardUID;
        if (status != Card.SCARD_S_SUCCESS)
            cardUID = "";
        else
            cardUID = BitConverter.ToString(receivedUID.Take(4).ToArray()).Replace("-", string.Empty).ToLower();
        return cardUID;
    }

    public List<string> GetReadersList()
    {
        int indx;
        int pcchReaders = 0;
        List<string> lstReaders = new List<string>();
        //Establish Context
        retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_USER, 0, 0, ref hContext);

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardEstablishContext");
        }

        // 2. List PC/SC card readers installed in the system

        retCode = Card.SCardListReaders(hContext, null, null, ref pcchReaders);

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardListReaders");
        }

        byte[] ReadersList = new byte[pcchReaders];

        // Fill reader list
        retCode = Card.SCardListReaders(hContext, null, ReadersList, ref pcchReaders);

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            throw new Exception("Error SCardListReaders");
        }

        string rName = "";
        indx = 0;


        while (ReadersList[indx] != 0)
        {

            while (ReadersList[indx] != 0)
            {
                rName += (char)ReadersList[indx];
                indx++;
            }


            lstReaders.Add(rName);
            rName = "";
            indx++;

        }
        return lstReaders;
    }

    public bool WriteBlock(string text, byte block)
    {
        char[] tmpStr = text.ToArray();
        int indx;
        if (AuthBlock(block))
        {
            ClearBuffers();
            SendBuffer[0] = 0xFF;                             // CLA
            SendBuffer[1] = 0xD6;                             // INS
            SendBuffer[2] = 0x00;                             // P1
            SendBuffer[3] = block;           // P2 : Starting Block No.
            SendBuffer[4] = 16;            // P3 : Data length

            for (indx = 0; indx <= tmpStr.Length - 1; indx++)
            {
                SendBuffer[indx + 5] = (byte)tmpStr[indx];
            }
            SendLength = SendBuffer[4] + 5;
            ReceiveLength = 0x02;

            retCode = SendAPDUandDisplay(2);

            if (retCode != Card.SCARD_S_SUCCESS)
                return false;
            else
                return true;
        }
        else
            return false;
    }

    public ReadOnlySpan<byte> ReadBlock(byte block)
    {
        if (!AuthBlock(block))
            return Span<byte>.Empty;

        ClearBuffers();
        SendBuffer[0] = 0xFF; // CLA 
        SendBuffer[1] = 0xB0;// INS
        SendBuffer[2] = 0x00;// P1
        SendBuffer[3] = block;// P2 : Block No.
        SendBuffer[4] = 16;// Le

        SendLength = 5;
        ReceiveLength = SendBuffer[4] + 2;

        retCode = SendAPDUandDisplay(2);

        if (retCode == -200)
        {
            return Array.Empty<byte>();
        }

        if (retCode == -202)
        {
            return Array.Empty<byte>();
        }

        if (retCode != Card.SCARD_S_SUCCESS)
        {
            return Array.Empty<byte>();
        }

        // Display data in text format
        return ReceiveBuffer.AsSpan()[..(ReceiveLength - 1)];
    }

    public bool Connect()
    {
        string readerName = GetReadersList()[0];
        connActive = true;
        retCode = Card.SCardConnect(hContext, readerName, Card.SCARD_SHARE_SHARED,
                             Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
        if (retCode != Card.SCARD_S_SUCCESS)
        {
            connActive = false;
            return false;
        }
        else
            return true;
    }

    public void Disconnect()
    {
        if (connActive)
        {
            retCode = Card.SCardDisconnect(hCard, Card.SCARD_UNPOWER_CARD);
        }
        //retCode = Card.SCardReleaseContext(hCard);
    }
    public string Transmit(byte[] buff)
    {
        string tmpStr = "";
        int indx;

        ClearBuffers();

        for (int i = 0; i < buff.Length; i++)
        {
            SendBuffer[i] = buff[i];
        }
        SendLength = 5;
        ReceiveLength = SendBuffer[^1] + 2;

        retCode = SendAPDUandDisplay(2);


        // Display data in text format
        for (indx = 0; indx <= ReceiveLength - 1; indx++)
        {
            tmpStr += Convert.ToChar(ReceiveBuffer[indx]);
        }

        return tmpStr;
    }
    public void Watch()
    {
        RdrState = new Card.SCARD_READERSTATE();
        readername = GetReadersList()[0];
        RdrState.RdrName = readername;

        states = new Card.SCARD_READERSTATE[1];
        states[0] = new Card.SCARD_READERSTATE();
        states[0].RdrName = readername;
        states[0].UserData = 0;
        states[0].RdrCurrState = Card.SCARD_STATE_EMPTY;
        states[0].RdrEventState = 0;
        states[0].ATRLength = 0;
        states[0].ATRValue = null;
        _worker = new BackgroundWorker();
        _worker.WorkerSupportsCancellation = true;
        _worker.DoWork += WaitChangeStatus;
        _worker.RunWorkerAsync();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}