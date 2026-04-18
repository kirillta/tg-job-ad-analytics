namespace TgJobAdAnalytics.Models.Reports.Html;

public readonly record struct ChartModel
{
    public ChartModel(Guid id, string type, DataModel data, bool isStacked = false)
    {
        Id = id;
        Data = data;
        IsStacked = isStacked;
        Type = type;
    }


    public string IdToken => "chart_" + Id.ToString("N");


    public Guid Id { get; }

    public DataModel Data { get; }

    public bool IsStacked { get; }

    public string Type { get; }


    public readonly record struct DataModel
    {
        public DataModel(List<string> labels, DatasetModel dataset, List<DatasetModel>? additionalDatasets = null)
        {
            Labels = labels;
            Dataset = dataset;
            AdditionalDatasets = additionalDatasets ?? [];
        }


        public List<string> Labels { get; }

        public DatasetModel Dataset { get; }

        public List<DatasetModel> AdditionalDatasets { get; }
    }


    public readonly record struct DatasetModel
    {
        public DatasetModel(string label, List<string> data, List<string> backgroundColor, List<string> borderColor, double tension = 0.1, string? typeOverride = null, string? yAxisId = null)
        {
            BackgroundColor = backgroundColor;
            BorderColor = borderColor;
            Data = data;
            Label = label;
            Tension = tension;
            TypeOverride = typeOverride;
            YAxisId = yAxisId;
        }


        public List<string> BackgroundColor { get; }

        public List<string> BorderColor { get; }

        public int BorderWidth { get; } = 1;

        public List<string> Data { get; }

        public bool Fill { get; } = false;

        public string Label { get; }

        public double Tension { get; }

        public string? TypeOverride { get; }

        public string? YAxisId { get; }
    }
}
