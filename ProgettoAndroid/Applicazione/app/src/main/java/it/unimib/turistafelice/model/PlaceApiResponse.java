package it.unimib.turistafelice.model;

import java.util.List;

public class PlaceApiResponse {

    private String next_page_token;
    private List<Place> results;
    private String status;


    public PlaceApiResponse(String next_page_token, List<Place> results, String status) {
        this.next_page_token = next_page_token;
        this.results = results;
        this.status = status;
    }

    public String getNext_page_token() {
        return next_page_token;
    }

    public void setNext_page_token(String next_page_token) {
        this.next_page_token = next_page_token;
    }

    public List<Place> getResults() {
        return results;
    }

    public void setResults(List<Place> results) {
        this.results = results;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    @Override
    public String toString() {
        return "PlaceApiResponse{" +
                "next_page_token='" + next_page_token + '\'' +
                ", results=" + results +
                ", status='" + status + '\'' +
                '}';
    }
}
