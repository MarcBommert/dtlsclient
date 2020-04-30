﻿using System;
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
  public class AsyncDtlsReplayWindow
  {
    private static Int64 VALID_SEQ_MASK = 0x0000FFFFFFFFFFFFL;

    private static Int64 WINDOW_SIZE = 64L;

    private long latestConfirmedSeq = -1;
    private long bitmap = 0;

    public Boolean ShouldDiscard(long seq)
    {
      if ((seq & VALID_SEQ_MASK) != seq)
      {
        return true;
      }

      if (seq <= latestConfirmedSeq)
      {
        Int32 diff = (Int32)(latestConfirmedSeq - seq);
        if (diff >= WINDOW_SIZE)
        {
          return true;
        }
        if ((bitmap & (1L << diff)) != 0)
        {
          return true;
        }
      }

      return false;
    }

    public void ReportAuthenticated(long seq)
    {
      if ((seq & VALID_SEQ_MASK) != seq)
      {
        throw new ArgumentException("'seq' out of range");
      }

      if (seq <= latestConfirmedSeq)
      {
        Int32 diff = (Int32)(latestConfirmedSeq - seq);
        if (diff < WINDOW_SIZE)
        {
          bitmap |= (1L << diff);
        }
      }
      else
      {
        long diff = seq - latestConfirmedSeq;
        if (diff >= WINDOW_SIZE)
        {
          bitmap = 1;
        }
        else
        {
          bitmap <<= (int)diff;        // for earlier JDKs
          bitmap |= 1;
        }
        latestConfirmedSeq = seq;
      }
    }

    public void Reset()
    {
      latestConfirmedSeq = -1;
      bitmap = 0;
    }
  }
}
