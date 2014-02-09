﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient.Events
{
    public enum EventCodes
    {
        BAD_USERNAME = 0x00,
        BAD_PASSWORD = 0x01
    }

    public class EventObject
    {
        public EventCodes ECode;

        public EventObject(EventCodes Code)
        {
            ECode = Code;
        }
    }
}
