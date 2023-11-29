namespace InMemoryCache
{
    /// <summary>
    /// 캐시 오브젝트 공통 클래스
    /// read, write 락, RU 카운터 내장
    /// </summary>
    internal class CacheElement<T>
    {
        /// <summary>set 때만 업데이트</summary>
        public DateTime UpdatedTime { get; protected set; }
        /// <summary>이 객체가 얼마나 오래 됐는지</summary>
        public int Age { get; protected set; }
        /// <summary>현재 writing 중인지</summary>
        public bool Writing { get; protected set; }
        /// <summary>read, write lock</summary>
        private object _lock;

        private T _data;
        public T data
        {
            get
            {
                Accessed();
                WaitWriting();
                return this._data;
            }
            set
            {
                Updated();
                AcquireWriting();
                _data = value;
                ReleaseWriting();
            }
        }


        public CacheElement()
        {
            this.Age = 0;
            this.UpdatedTime = DateTime.MinValue;
            this._lock = new object();
            this.data = default(T);
        }

        public CacheElement(T data) : this()
        {
            this.data = data;
        }

        protected void WaitWriting()
        {
            while (this.Writing)
            {
                //spin wait
                //Thread.Sleep(1);
            }
        }

        protected void Accessed()
        {
            lock (this._lock)
            {
                this.Age = 0;
            }
        }
        protected void Updated()
        {
            lock (this._lock)
            {
                this.Age = 0;
                this.UpdatedTime = DateTime.Now;
            }
        }
        protected void AcquireWriting()
        {
            WaitWriting();
            lock (this._lock)
            {
                this.Writing = true;
            }
        }
        protected void ReleaseWriting()
        {
            lock (this._lock)
            {
                this.Writing = false;
            }
        }

        /// <summary>
        /// 얼마나 오랫동안 접근하지 않았는지 누적시킴
        /// </summary>
        /// <returns></returns>
        public int GetOlder()
        {
            lock (this._lock)
            {
                this.Age += 1;
                return this.Age;
            }
        }
    }
}
