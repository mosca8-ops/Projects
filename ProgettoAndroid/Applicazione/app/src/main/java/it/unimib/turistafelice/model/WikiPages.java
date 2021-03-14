package it.unimib.turistafelice.model;


public class WikiPages {
    private int pageId;
    private String title;
    private String extract;

    public WikiPages(int pageId, String title, String extract) {
        this.pageId = pageId;
        this.title = title;
        this.extract = extract;
    }

    public int getPageId() {
        return pageId;
    }

    public void setPageId(int pageId) {
        this.pageId = pageId;
    }

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public String getExtract() {
        return extract;
    }

    public void setExtract(String extract) {
        this.extract = extract;
    }

    @Override
    public String toString() {
        return "WikiPages{" +
                "pageId=" + pageId +
                ", title='" + title + '\'' +
                ", extract='" + extract + '\'' +
                '}';
    }
}
