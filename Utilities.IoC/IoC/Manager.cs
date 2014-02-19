﻿/*
Copyright (c) 2014 <a href="http://www.gutgames.com">James Craig</a>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Utilities.IoC.Default;
using Utilities.IoC.Interfaces;

#endregion Usings

namespace Utilities.IoC
{
    /// <summary>
    /// IoC manager class
    /// </summary>
    public class Manager : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        protected Manager()
        {
            foreach (FileInfo File in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.dll", SearchOption.TopDirectoryOnly))
            {
                AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(File.FullName));
            }
            List<Type> Bootstrappers = new List<Type>();
            foreach (Assembly Assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Bootstrappers.AddRange(Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IBootstrapper))
                                                                        && x.IsClass
                                                                        && !x.IsAbstract
                                                                        && !x.ContainsGenericParameters
                                                                        && !x.Namespace.StartsWith("UTILITIES", StringComparison.OrdinalIgnoreCase)));
            }
            if (Bootstrappers.Count == 0)
            {
                Bootstrappers.Add(typeof(DefaultBootstrapper));
            }
            InternalBootstrapper = (IBootstrapper)Activator.CreateInstance(Bootstrappers[0]);

            List<Type> Modules = new List<Type>();
            foreach (Assembly Assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Modules.AddRange(Assembly.GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IModule))
                                                                        && x.IsClass
                                                                        && !x.IsAbstract
                                                                        && !x.ContainsGenericParameters));
            }
            List<IModule> ModuleObjects = new List<IModule>();
            foreach (Type Module in Modules)
            {
                ModuleObjects.Add(((IModule)Activator.CreateInstance(Module)));
            }
            foreach (IModule Module in ModuleObjects.OrderBy(x => x.Order))
            {
                Module.Load(InternalBootstrapper);
            }
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets the instance of the manager
        /// </summary>
        public static IBootstrapper Bootstrapper
        {
            get
            {
                if (_Instance == null)
                {
                    lock (Temp)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new Manager();
                        }
                    }
                }
                return _Instance.InternalBootstrapper;
            }
        }

        /// <summary>
        /// Bootstrapper object
        /// </summary>
        protected IBootstrapper InternalBootstrapper { get; private set; }

        private static Manager _Instance = new Manager();
        private static object Temp = 1;

        #endregion Properties

        #region Functions

        /// <summary>
        /// Disposes of the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Displays information about the IoC container in string form
        /// </summary>
        /// <returns>Information about the IoC container</returns>
        public override string ToString()
        {
            return Bootstrapper.Name;
        }

        /// <summary>
        /// Disposes of the object
        /// </summary>
        /// <param name="Managed">
        /// Determines if all objects should be disposed or just managed objects
        /// </param>
        protected virtual void Dispose(bool Managed)
        {
            if (InternalBootstrapper != null)
            {
                InternalBootstrapper.Dispose();
                InternalBootstrapper = null;
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Manager()
        {
            Dispose(false);
        }

        #endregion Functions
    }
}