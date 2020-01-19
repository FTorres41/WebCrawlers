using System;

namespace RSBM.Models
{
    public class ConfigRobot
    {
        public virtual int Id { get; set; }
        public virtual string Mode { get; set; }
        public virtual int? IntervalMin { get; set; }
        public virtual int? ScheduleTime { get; set; }
        public virtual char Active { get; set; }
        public virtual string Name { get; set; }
        public virtual DateTime? PreTypedDate { get; set; }
        public virtual DateTime? LastDate { get; set; }
        public virtual int? NumLicitLast { get; set; }
        public virtual char Status { get; set; }
        public virtual DateTime? NextDate { get; set; }

    }
}
