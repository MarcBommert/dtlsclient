﻿using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
 * Mobius Software LTD
 * Copyright 2015-2017, Mobius Software LTD
 *
 * This is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
 * 02110-1301 USA, or see the FSF site: http://www.fsf.org.
 */

namespace com.mobius.software.windows.iotbroker.network.dtls
{
  public class AsyncDtlsEpoch
  {
    private AsyncDtlsReplayWindow replayWindow = new AsyncDtlsReplayWindow();

    private int epoch;
    private TlsCipher cipher;

    private long sequenceNumber = 0;

    public AsyncDtlsEpoch(int epoch, TlsCipher cipher)
    {
      if (epoch < 0)
      {
        throw new ArgumentException("'epoch' must be >= 0");
      }
      if (cipher == null)
      {
        throw new ArgumentException("'cipher' cannot be null");
      }

      this.epoch = epoch;
      this.cipher = cipher;
    }

    public long allocateSequenceNumber()
    {
      return sequenceNumber++;
    }

    public TlsCipher getCipher()
    {
      return cipher;
    }

    public int Epoch
    {
      get
      {
        return epoch;
      }
    }

    public AsyncDtlsReplayWindow ReplayWindow
    {
      get
      {
        return replayWindow;
      }
    }

    public long SequenceNumber
    {
      get
      {
        return sequenceNumber;
      }
    }
  }
}
