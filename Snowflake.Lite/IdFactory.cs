using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace Snowflake.Lite
{
    /// <summary>
    /// Generates a series of ID based on Twitter's Snowflake algorithm.
    /// </summary>
    public sealed class IdFactory
    {
        #region Fields

        /// <summary>
        /// Gets the maximum available value of worker ID.
        /// </summary>
        public const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);

        /// <summary>
        /// Gets the maximum available value of data center ID.
        /// </summary>
        public const long MaxDataCenterId = -1L ^ (-1L << DataCenterIdBits);

        internal const int WorkerIdBits = 5;
        internal const int DataCenterIdBits = 5;
        internal const int SequenceBits = 12;

        internal const int WorkerIdShift = SequenceBits;
        internal const int DataCenterIdShift = SequenceBits + WorkerIdBits;
        internal const int TimestampLeftShift = SequenceBits + WorkerIdBits + DataCenterIdBits;

        internal const long Epoch = 1288834974657L;
        internal const long SequenceMask = -1L ^ (-1L << SequenceBits);

        internal static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private long _sequence = 0L;
        private long _lastTimestamp = -1L;

        private SpinLock _lock;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current sequence number of this instance.
        /// </summary>
        public long Sequence
        {
            get { return _sequence; }
        }

        /// <summary>
        /// Gets the unique worker ID of this instance.
        /// </summary>
        public long WorkerId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the unique data center ID of this instance.
        /// </summary>
        public long DataCenterId
        {
            get;
            private set;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a new instance of <see cref="IdFactory"/> with default settings.
        /// </summary>
        public IdFactory()
        {
            var ipList = Dns.GetHostAddresses(Dns.GetHostName()).Select(a => a.GetAddressBytes());

            this.WorkerId = ipList.Max(a => a.Last()) % (MaxWorkerId + 1);
            this.DataCenterId = ipList.Max(a => a.First()) % (MaxDataCenterId + 1);

            this._lock = new SpinLock();
        }

        /// <summary>
        /// Initialize a new instance of <see cref="IdFactory"/>.
        /// </summary>
        /// <param name="workerId">The worker ID of this instance.</param>
        /// <param name="dataCenterId">The data center ID of this instance.</param>
        /// <param name="sequence">The sequence of where the instance starts at.</param>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="workerId"/> is greater than <see cref="MaxWorkerId"/> or less than 0.</para>
        /// <para>-- or --</para>
        /// <para><paramref name="dataCenterId"/> is greater than <see cref="MaxDataCenterId"/> or less than 0.</para>
        /// </exception>
        public IdFactory(long workerId, long dataCenterId, long sequence)
        {
            if (workerId > MaxWorkerId || workerId < 0)
                throw new ArgumentException(string.Format("Cannot be greater than {0} or less than 0", MaxWorkerId), "workerId");

            if (dataCenterId > MaxDataCenterId || dataCenterId < 0)
                throw new ArgumentException(string.Format("Cannot be greater than {0} or less than 0", MaxDataCenterId), "dataCenterId");

            this.WorkerId = workerId;
            this.DataCenterId = dataCenterId;

            this._sequence = sequence;
            this._lock = new SpinLock();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets next ID.
        /// </summary>
        /// <returns>The next available ID.</returns>
        public long GetNextId()
        {
            var lockTaken = false;
            var timestamp = GenerateTimestamp();

            this._lock.Enter(ref lockTaken);

            if (this._lastTimestamp != timestamp)
                this._sequence = 0;
            else
            {
                this._sequence = (this._sequence + 1) & SequenceMask;

                if (this._sequence == 0)
                    timestamp = GetNextTimestamp(this._lastTimestamp);
            }

            this._lastTimestamp = timestamp;

            if (lockTaken) this._lock.Exit();

            checked
            {
                return ((timestamp - Epoch) << TimestampLeftShift)
                    | (DataCenterId << DataCenterIdShift)
                    | (WorkerId << WorkerIdShift)
                    | this._sequence;
            }
        }

        #endregion

        #region Others

        private static long GetNextTimestamp(long lastTimestamp)
        {
            var timestamp = 0L;

            do { timestamp = GenerateTimestamp(); }
            while (timestamp <= lastTimestamp);

            return timestamp;
        }

        private static long GenerateTimestamp()
        {
            return checked((long)(DateTime.UtcNow - EpochStart).TotalMilliseconds);
        }

        #endregion
    }
}