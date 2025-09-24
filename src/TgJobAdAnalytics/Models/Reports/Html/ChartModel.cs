namespace TgJobAdAnalytics.Models.Reports.Html;

public readonly record struct ChartModel
{
    public ChartModel(Guid id, string type, DataModel data)
    {
        Id = id;
        Data = data;
        Type = type;
    }


    public string IdToken => "chart_" + Id.ToString("N");


    public Guid Id { get; }

    public DataModel Data { get; }

    public string Type { get; }


    public readonly record struct DataModel
    {
        public DataModel(List<string> labels, DatasetModel dataset)
        {
            Labels = labels;
            Dataset = dataset;
        }


        public List<string> Labels { get; }

        public DatasetModel Dataset { get; }
    }


    public readonly record struct DatasetModel
    {
        public DatasetModel(string label, List<string> data, List<string> backgroundColor, List<string> borderColor)
        {
            BackgroundColor = backgroundColor;
            BorderColor = borderColor;
            Data = data;
            Label = label;
        }


        public List<string> BackgroundColor { get; }

        public List<string> BorderColor { get; }

        public int BorderWidth { get; } = 1;

        public List<string> Data { get; }

        public bool Fill { get; } = false;

        public string Label { get; }

        public double Tension { get; } = 0.1;
    }
}
