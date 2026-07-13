using System;
using System.Collections.Generic;
using System.Text;

namespace HomeNotes.Core.Interfaces
{
    public interface IGetUserCurrentId
    {
        Guid UserId { get; }
    }
}
