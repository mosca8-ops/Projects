package it.unimib.turistafelice.model;

import java.util.List;

public class WikiQuerySearch {

    private List<WikiSearch> search;

    public WikiQuerySearch(List<WikiSearch> search) {
        this.search = search;
    }

    public List<WikiSearch> getSearch() {
        return search;
    }

    public void setSearch(List<WikiSearch> search) {
        this.search = search;
    }

    @Override
    public String toString() {
        return "WikiQuerySearch{" +
                "search=" + search +
                '}';
    }
}
