package it.unimib.turistafelice.model;

public class WikiSearch {

    private String title;
    private int pageId;

    public WikiSearch(String title, int pageId) {
        this.title = title;
        this.pageId = pageId;
    }

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public int getPageId() {
        return pageId;
    }

    public void setPageId(int pageId) {
        this.pageId = pageId;
    }

    @Override
    public String toString() {
        return "WikiSearch{" +
                "title='" + title + '\'' +
                ", pageId=" + pageId +
                '}';
    }
}
