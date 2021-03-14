package it.unimib.turistafelice.model;

public class WikiTextApiResponse {
    private WikiQueryText query;

    public WikiTextApiResponse() {
    }

    public WikiTextApiResponse(WikiQueryText query) {
        this.query = query;
    }

    public WikiQueryText getQuery() {
        return query;
    }

    public void setQuery(WikiQueryText query) {
        this.query = query;
    }

    @Override
    public String toString() {
        return "WikiTextApiResponse{" +
                "query=" + query +
                '}';
    }
}
