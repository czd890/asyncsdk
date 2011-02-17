﻿using System;
using System.Collections.Generic;
using System.Text;
using ASync.eTermCommand;

namespace ASync.eTermCommand.SSException {
    /// <summary>
    /// 错误的office代码
    /// </summary>
    public class SSOfficeException:SdkException {
        /// <summary>
        /// Initializes a new instance of the <see cref="SSOfficeException"/> class.
        /// </summary>
        public SSOfficeException() : base("错误的office代码") { }
    }
}
