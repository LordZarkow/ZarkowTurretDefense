using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    public class NetDataObjectHandler
    {
        private readonly ZDO _zDataObject;
        private uint _dataRevision = 0;

        public bool HasDataToRead => ((_zDataObject != null) && _dataRevision != _zDataObject.DataRevision);
        public ZDO Data => _zDataObject;

        public NetDataObjectHandler(ZDO zDataObject)
        {
            _zDataObject = zDataObject;
            _dataRevision = 1;
        }

        public void ReceiveDone()
        {
            if (_zDataObject == null)
            {
                _dataRevision = 0;
                return;
            }

            _dataRevision = _zDataObject.DataRevision;
        }
    }
}
