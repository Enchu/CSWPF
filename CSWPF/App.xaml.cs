using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CSWPF
{
    public partial class App : Application
    {
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        public static CancellationToken Token => App.cts.Token;
    }
}