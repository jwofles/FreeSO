﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.HIT;

namespace FSO.HIT.Model
{
    /// <summary>
    /// Groups related HIT resources, like the tsov2 series or newmain.
    /// </summary>
    public class HITResourceGroup
    {
        public EVT evt;
        public HITFile hit;
        public HSM hsm;
    }
}
