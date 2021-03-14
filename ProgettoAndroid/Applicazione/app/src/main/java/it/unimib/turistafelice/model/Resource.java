package it.unimib.turistafelice.model;

public class Resource<T> {
    private T data;
    private String status;

    public Resource(T data, String status) {
        this.data = data;
        this.status = status;
    }

    public Resource() { }

    public T getData() {
        return data;
    }

    public void setData(T data) {
        this.data = data;
    }

    public String getStatus() {
        return status;
    }

    public void setStatus(String status) {
        this.status = status;
    }

    @Override
    public String toString() {
        return "Resource{" +
                "data=" + data +
                ", status='" + status + '\'' +
                '}';
    }
}
