package it.unimib.turistafelice.model;

import java.util.List;

public class UnsplashApiResponse {

    private List<UnsplashResults> results;

    public UnsplashApiResponse(List<UnsplashResults> results) {
        this.results = results;
    }

    public List<UnsplashResults> getResults() {
        return results;
    }

    public void setResults(List<UnsplashResults> results) {
        this.results = results;
    }

    @Override
    public String toString() {
        return "UnsplashApiResponse{" +
                "results=" + results +
                '}';
    }
}
