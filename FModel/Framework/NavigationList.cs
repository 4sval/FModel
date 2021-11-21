using System.Collections.Generic;

namespace FModel.Framework
{
    public class NavigationList<T> : List<T>
    {
        private int _currentIndex = 0;
        public int CurrentIndex
        {
            get
            {
                if (_currentIndex > Count - 1) { _currentIndex = Count - 1; }
                if (_currentIndex < 0) { _currentIndex = 0; }
                return _currentIndex;
            }
            set { _currentIndex = value; }
        }

        public T MoveNext
        {
            get { _currentIndex++; return this[CurrentIndex]; }
        }

        public T MovePrevious
        {
            get { _currentIndex--; return this[CurrentIndex]; }
        }

        public T Current
        {
            get { return this[CurrentIndex]; }
        }
    }
}
