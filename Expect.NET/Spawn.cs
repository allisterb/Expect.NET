﻿using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Expect
{
    public class Spawn
    {
        internal Spawn(IBackendFactory backendFactory)
        {
            _backend = backendFactory.CreateBackend();
        }

        /// <summary>
        /// Executes code when expected string is found by Expect function
        /// </summary>
        public delegate void ExpectedHandler();

        /// <summary>
        /// Executes code when expected string is found by Expect function.
        /// Receives session output to handle.
        /// </summary>
        /// <param name="output">session output with expected pattern</param>
        public delegate void ExpectedHandlerWithOutput(string output);

        /// <summary>
        /// Sends characters to the session.
        /// </summary>
        /// <remarks>
        /// To send enter you have to add '\n' at the end.
        /// </remarks>
        /// <example>
        /// Send("cmd.exe\n");
        /// </example>
        /// <param name="command">String to be sent to session</param>
        public void Send(string command) { _backend.Write(command); }

        /// <summary>
        /// Waits until query is printed on session output and 
        /// executes handler
        /// <para>
        /// This is wrapped async function. All await calls use ConfigureAwait(false), that should prevent 
        /// from deadlock. Anyway, be aware when using it.
        /// </para>
        /// </summary>
        /// <param name="query">expected output</param>
        /// <param name="handler">action to be performed</param>
        /// <exception cref="System.TimeoutException">Thrown when query is not find for given
        /// amount of time</exception>
        public void Expect(string query, ExpectedHandler handler)
        {
            try
            {
                ExpectAsync(query, (s) => handler()).Wait();
            }
            catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        /// <summary>
        /// Waits until query is printed on session output and 
        /// executes handler. The output including expected query is
        /// passed to handler.
        /// <para>
        /// This is wrapped async function. All await calls use ConfigureAwait(false), that should prevent 
        /// from deadlock. Anyway, be aware when using it.
        /// </para>
        /// </summary>
        /// <param name="query">expected output</param>
        /// <param name="handler">action to be performed, it accepts session output as ana argument</param>
        /// <exception cref="System.TimeoutException">Thrown when query is not find for given
        /// amount of time</exception>
        public void Expect(string query, ExpectedHandlerWithOutput handler)
        {
            try
            {
                ExpectAsync(query, (s) => handler(s)).Wait();
            }
            catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        [Obsolete("Use GetTimeout()")]
        public int getTimeout()
        {
            return GetTimeout();
        }

        /// <summary>
        /// Returns configured timeout value for Expect function
        /// </summary>
        /// <returns>timeout value in miliseconds for Expect function</returns>
        public int GetTimeout()
        {
            return _timeout;
        }

        [Obsolete("Use SetTimeout()")]
        public void setTimeout(int timeout)
        {
            SetTimeout(timeout);
        }

        /// <summary>
        /// Sets timeout value for Expect function
        /// </summary>
        /// <param name="timeout">timeout value in miliseconds for Expect function</param>
        public void SetTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException("timeout", "Value must be larger than zero");
            }
            _timeout = timeout;
        }

        private IBackend _backend;
        private string _output;
        private int _timeout = 2500;


        /// <summary>
        /// Waits until query is printed on session output and 
        /// executes handler
        /// </summary>
        /// <param name="query">expected output</param>
        /// <param name="handler">action to be performed</param>
        /// <exception cref="System.TimeoutException">Thrown when query is not find for given
        /// amount of time</exception>
        public async Task ExpectAsync(string query, ExpectedHandler handler)
        {
            await ExpectAsync(query, s => handler()).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits until query is printed on session output and 
        /// executes handler. The output including expected query is
        /// passed to handler.
        /// </summary>
        /// <param name="query">expected output</param>
        /// <param name="handler">action to be performed, it accepts session output as ana argument</param>
        /// <exception cref="System.TimeoutException">Thrown when query is not find for given
        /// amount of time</exception>
        public async Task ExpectAsync(string query, ExpectedHandlerWithOutput handler)
        {
            Task timeoutTask = null;
            if (_timeout > 0)
            {
                timeoutTask = Task.Delay(_timeout);
            }
            _output = "";
            bool expectedQueryFound = false;
            while (!expectedQueryFound)
            {
                Task<string> task = _backend.ReadAsync();
                IList<Task> tasks = new List<Task>();
                tasks.Add(task);
                if (timeoutTask != null)
                {
                    tasks.Add(timeoutTask);
                }
                Task any = await Task.WhenAny(tasks).ConfigureAwait(false);
                if (task == any)
                {
                    _output += await task.ConfigureAwait(false);
                    expectedQueryFound = Regex.Match(_output, query).Success;
                    if (expectedQueryFound)
                    {
                        handler(_output);
                    }
                }
                else
                {
                    throw new TimeoutException();
                }
            }
        }
    }
}
