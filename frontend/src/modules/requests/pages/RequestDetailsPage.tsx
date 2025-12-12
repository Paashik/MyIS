import React, { useEffect, useState } from "react";
import { Alert, Button, Descriptions, Spin, Tabs, Typography, Result, Space } from "antd";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  RequestCommentDto,
  RequestDto,
  RequestHistoryItemDto,
} from "../api/types";
import {
  addRequestComment,
  getRequest,
  getRequestComments,
  getRequestHistory,
} from "../api/requestsApi";
import { RequestStatusBadge } from "../components/RequestStatusBadge";
import { RequestHistoryTimeline } from "../components/RequestHistoryTimeline";
import { RequestCommentsPanel } from "../components/RequestCommentsPanel";
import { RequestBodyRenderer } from "../components/RequestBodyRenderer";
import { useCan } from "../../../core/auth/permissions";
import { t } from "../../../core/i18n/t";

const { Title, Text } = Typography;

type LoadState =
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "notFound" }
  | { kind: "error"; message: string };

export const RequestDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const canEdit = useCan("Requests.Edit") || useCan("Requests.Create");

  type RequestsDirectionSegment = "incoming" | "outgoing";

  const returnContext = (() => {
    const sp = new URLSearchParams(location.search);
    const rawDirection = (sp.get("direction") || "").trim().toLowerCase();
    const direction: RequestsDirectionSegment = rawDirection === "outgoing" ? "outgoing" : "incoming";

    const rawType = sp.get("type");
    const type = ((rawType || "").trim() || "all");

    return { direction, type };
  })();

  const [request, setRequest] = useState<RequestDto | null>(null);
  const [state, setState] = useState<LoadState>({ kind: "loading" });

  const [history, setHistory] = useState<RequestHistoryItemDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);

  const [comments, setComments] = useState<RequestCommentDto[]>([]);
  const [commentsLoading, setCommentsLoading] = useState(false);
  const [commentsError, setCommentsError] = useState<string | null>(null);
  const [addingComment, setAddingComment] = useState(false);

  useEffect(() => {
    if (!id) {
      setState({ kind: "notFound" });
      return;
    }

    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });
      try {
        const dto = await getRequest(id);
        if (cancelled) return;
        setRequest(dto);
        setState({ kind: "loaded" });
      } catch (error: any) {
        if (cancelled) return;

        // Пытаемся эвристически определить 404 по тексту ошибки
        const message =
          error instanceof Error
            ? error.message
            : t("requests.details.error.load.unknown");
        if (message.includes("404")) {
          setState({ kind: "notFound" });
        } else {
          setState({ kind: "error", message });
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [id]);

  useEffect(() => {
    if (!id) {
      return;
    }

    let cancelled = false;

    const loadHistory = async () => {
      setHistoryLoading(true);
      setHistoryError(null);
      try {
        const items = await getRequestHistory(id);
        if (cancelled) return;
        setHistory(items);
      } catch (error) {
        if (cancelled) return;
        const message =
          error instanceof Error
            ? error.message
            : t("requests.details.history.error.unknown");
        setHistoryError(message);
      } finally {
        setHistoryLoading(false);
      }
    };

    const loadComments = async () => {
      setCommentsLoading(true);
      setCommentsError(null);
      try {
        const items = await getRequestComments(id);
        if (cancelled) return;
        setComments(items);
      } catch (error) {
        if (cancelled) return;
        const message =
          error instanceof Error
            ? error.message
            : t("requests.details.comments.error.unknown");
        setCommentsError(message);
      } finally {
        setCommentsLoading(false);
      }
    };

    void loadHistory();
    void loadComments();

    return () => {
      cancelled = true;
    };
  }, [id]);

  const handleReload = () => {
    if (!id) {
      navigate("/requests");
      return;
    }
    // Простейший способ перезагрузить — перезагрузить страницу браузера
    // или можно использовать navigate(0) в React Router v6.4+,
    // но здесь оставим переход на тот же URL.
    navigate(0 as any);
  };

  const handleBackToList = () => {
    navigate(`/requests/${encodeURIComponent(returnContext.direction)}?type=${encodeURIComponent(returnContext.type)}`);
  };

  const handleEdit = () => {
    if (!id) return;
    navigate(
      `/requests/${encodeURIComponent(id)}/edit?direction=${encodeURIComponent(returnContext.direction)}&type=${encodeURIComponent(returnContext.type)}`
    );
  };

  const handleAddComment = async (text: string) => {
    if (!id) return;
    setAddingComment(true);
    try {
      const created = await addRequestComment(id, { text });
      // Обновляем локальное состояние комментариев
      setComments((prev) => [...prev, created]);
    } catch (error) {
      const message =
        error instanceof Error
          ? error.message
          : t("requests.details.comments.add.error");
      setCommentsError(message);
    } finally {
      setAddingComment(false);
    }
  };

  if (state.kind === "notFound") {
    return (
      <Result
        status="404"
        title={t("requests.details.notFound.title")}
        subTitle={t("requests.details.notFound.subtitle")}
        extra={
          <Button
            data-testid="request-details-not-found-back-button"
            type="primary"
            onClick={handleBackToList}
          >
            {t("requests.details.notFound.back")}
          </Button>
        }
      />
    );
  }

  if (state.kind === "loading") {
    return (
      <div
        data-testid="request-details-loading"
        style={{
          minHeight: "40vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin tip={t("requests.details.loading")} />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          data-testid="request-details-error-alert"
          type="error"
          message={t("requests.details.error.load.title")}
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button data-testid="request-details-retry-button" onClick={handleReload}>
          {t("common.actions.retry")}
        </Button>
      </div>
    );
  }

  if (!request) {
    return null;
  }

  const createdAt = new Date(request.createdAt);
  const updatedAt = new Date(request.updatedAt);
  const dueDate = request.dueDate ? new Date(request.dueDate) : null;

  return (
    <div data-testid="request-details-page">
      <Space
        style={{ marginBottom: 16, display: "flex", justifyContent: "space-between" }}
        align="center"
      >
        <Title level={2} style={{ margin: 0 }}>
          {request.title}
        </Title>

        <Space>
          {canEdit && (
            <Button
              data-testid="request-details-edit-button"
              onClick={handleEdit}
              type="primary"
            >
              {t("common.actions.edit")}
            </Button>
          )}
          <Button data-testid="request-details-back-button" onClick={handleBackToList}>
            {t("requests.details.actions.backToList")}
          </Button>
        </Space>
      </Space>

      <div style={{ marginBottom: 24 }}>
        <Descriptions column={2} bordered size="small">
          <Descriptions.Item label={t("requests.details.fields.id")} span={2}>
            <Text code>{request.id}</Text>
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.type")}>
            <Text strong>{request.requestTypeCode}</Text> {request.requestTypeName}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.status")}>
            <RequestStatusBadge
              statusCode={request.requestStatusCode}
              statusName={request.requestStatusName}
            />
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.initiator")}>
            {request.initiatorFullName || request.initiatorId}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.createdAt")}>
            {createdAt.toLocaleDateString()} {createdAt.toLocaleTimeString()}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.updatedAt")}>
            {updatedAt.toLocaleDateString()} {updatedAt.toLocaleTimeString()}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.dueDate")}>
            {dueDate
              ? `${dueDate.toLocaleDateString()} ${dueDate.toLocaleTimeString()}`
              : t("requests.details.value.notSet")}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.externalId")}>
            {request.externalReferenceId || (
              <Text type="secondary">{t("requests.details.value.notSet")}</Text>
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.relatedType")}>
            {request.relatedEntityType || (
              <Text type="secondary">{t("requests.details.value.notSet")}</Text>
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t("requests.details.fields.relatedId")}>
            {request.relatedEntityId || (
              <Text type="secondary">{t("requests.details.value.notSet")}</Text>
            )}
          </Descriptions.Item>
        </Descriptions>
      </div>

      <Tabs
        data-testid="request-details-tabs"
        defaultActiveKey="details"
        items={[
          {
            key: "details",
            label: t("requests.details.tabs.details"),
            children: (
              <RequestBodyRenderer
                mode="details"
                requestTypeCode={request.requestTypeCode}
                request={request}
              />
            ),
          },
          {
            key: "history",
            label: t("requests.details.tabs.history"),
            children: (
              <>
                {historyError && (
                  <Alert
                    data-testid="request-details-history-error-alert"
                    type="error"
                    message={t("requests.details.history.error.title")}
                    description={historyError}
                    showIcon
                    style={{ marginBottom: 16 }}
                  />
                )}
                <RequestHistoryTimeline items={history} loading={historyLoading} />
              </>
            ),
          },
          {
            key: "comments",
            label: t("requests.details.tabs.comments"),
            children: (
              <RequestCommentsPanel
                comments={comments}
                loading={commentsLoading}
                adding={addingComment}
                error={commentsError}
                onAddComment={handleAddComment}
              />
            ),
          },
        ]}
      />
    </div>
  );
};
