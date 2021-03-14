package it.unimib.turistafelice.model;

public class WikiTitleApiResponse {
    private String batchcomplete;
    private WikiQuerySearch query;

    public WikiTitleApiResponse(String batchcomplete, WikiQuerySearch query) {
        this.batchcomplete = batchcomplete;
        this.query = query;
    }

    public String getBatchcomplete() {
        return batchcomplete;
    }

    public void setBatchcomplete(String batchcomplete) {
        this.batchcomplete = batchcomplete;
    }

    public WikiQuerySearch getQuery() {
        return query;
    }

    public void setQuery(WikiQuerySearch query) {
        this.query = query;
    }

    @Override
    public String toString() {
        return "WikiApiResponse{" +
                "batchcomplete='" + batchcomplete + '\'' +
                ", query=" + query +
                '}';
    }
}
