import { t } from "../../../core/i18n/t";
import { CustomerOrderListItemDto, GetCustomerOrdersParams, PagedResultDto } from "./types";

async function httpRequest<TResponse>(input: string, init?: RequestInit): Promise<TResponse> {
  const response = await fetch(input, {
    credentials: "include",
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const text = await response.text().catch(() => "");
    const message =
      text ||
      t("requests.api.error.http", {
        status: response.status,
        statusText: response.statusText,
      });
    throw new Error(message);
  }

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.toLowerCase().includes("application/json");

  if (!isJson) {
    const text = await response.text().catch(() => "");
    const snippet = text ? text.slice(0, 200) : "";
    throw new Error(
      t("requests.api.error.expectedJson", {
        contentType,
        snippet,
      })
    );
  }

  return (await response.json()) as TResponse;
}

function buildQueryString(params?: GetCustomerOrdersParams): string {
  if (!params) return "";

  const searchParams = new URLSearchParams();

  if (params.q) {
    searchParams.set("q", params.q);
  }
  if (params.customerId) {
    searchParams.set("customerId", params.customerId);
  }
  if (typeof params.pageNumber === "number") {
    searchParams.set("pageNumber", String(params.pageNumber));
  }
  if (typeof params.pageSize === "number") {
    searchParams.set("pageSize", String(params.pageSize));
  }

  const query = searchParams.toString();
  return query ? `?${query}` : "";
}

export async function getCustomerOrders(
  params?: GetCustomerOrdersParams
): Promise<PagedResultDto<CustomerOrderListItemDto>> {
  const query = buildQueryString(params);
  return httpRequest<PagedResultDto<CustomerOrderListItemDto>>(
    `/api/customers/orders${query}`,
    {
      method: "GET",
    }
  );
}
