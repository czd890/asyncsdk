﻿using System;
using System.Collections.Generic;
using System.Text;
using ASync.eTermCommand;

namespace ASync.eTermCommand.FDException {
    /// <summary>
    /// FD返回城市对不正确
    /// </summary>
    public class FDCityPairException:SdkException {
        /// <summary>
        /// Initializes a new instance of the <see cref="FDCityPairException"/> class.
        /// </summary>
        public FDCityPairException() : base("FD返回城市对不正确") { }
    }
}
