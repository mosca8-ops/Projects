package it.unimib.turistafelice.model;

import java.util.List;

public class WikiQueryText {
    private List<WikiPages> pages;

    public WikiQueryText(List<WikiPages> pages) {
        this.pages = pages;
    }

    public List<WikiPages> getPages() {
        return pages;
    }

    public void setPages(List<WikiPages> pages) {
        this.pages = pages;
    }

    @Override
    public String toString() {
        return "WikiQueryText{" +
                "pages=" + pages +
                '}';
    }
}
