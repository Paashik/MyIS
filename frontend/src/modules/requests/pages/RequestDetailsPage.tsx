import React, { useEffect, useState } from "react";
import { Alert, Button, Descriptions, Spin, Tabs, Typography, Result, Space } from "antd";
import { useNavigate, useParams } from "react-router-dom";
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
import { useCan } from "../../../core/auth/permissions";

const { Title, Text } = Typography;

type LoadState =
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "notFound" }
  | { kind: "error"; message: string };

export const RequestDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const canEdit = useCan("Requests.Edit") || useCan("Requests.Create");

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
          error instanceof Error ? error.message : "Неизвестная ошибка при загрузке заявки";
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
          error instanceof Error ? error.message : "Не удалось загрузить историю заявки";
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
          error instanceof Error ? error.message : "Не удалось загрузить комментарии";
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
    navigate("/requests");
  };

  const handleEdit = () => {
    if (!id) return;
    navigate(`/requests/${encodeURIComponent(id)}/edit`);
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
        error instanceof Error ? error.message : "Не удалось добавить комментарий";
      setCommentsError(message);
    } finally {
      setAddingComment(false);
    }
  };

  if (state.kind === "notFound") {
    return (
      <Result
        status="404"
        title="Заявка не найдена"
        subTitle="Заявка не существует или была удалена."
        extra={
          <Button type="primary" onClick={handleBackToList}>
            Вернуться к списку заявок
          </Button>
        }
      />
    );
  }

  if (state.kind === "loading") {
    return (
      <div
        style={{
          minHeight: "40vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin tip="Загрузка заявки..." />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          type="error"
          message="Не удалось загрузить заявку"
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button onClick={handleReload}>Повторить попытку</Button>
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
    <div>
      <Space
        style={{ marginBottom: 16, display: "flex", justifyContent: "space-between" }}
        align="center"
      >
        <Title level={2} style={{ margin: 0 }}>
          {request.title}
        </Title>

        <Space>
          {canEdit && (
            <Button onClick={handleEdit} type="primary">
              Редактировать
            </Button>
          )}
          <Button onClick={handleBackToList}>К списку заявок</Button>
        </Space>
      </Space>

      <div style={{ marginBottom: 24 }}>
        <Descriptions column={2} bordered size="small">
          <Descriptions.Item label="ID" span={2}>
            <Text code>{request.id}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Тип">
            <Text strong>{request.requestTypeCode}</Text> {request.requestTypeName}
          </Descriptions.Item>
          <Descriptions.Item label="Статус">
            <RequestStatusBadge
              statusCode={request.requestStatusCode}
              statusName={request.requestStatusName}
            />
          </Descriptions.Item>
          <Descriptions.Item label="Инициатор">
            {request.initiatorFullName || request.initiatorId}
          </Descriptions.Item>
          <Descriptions.Item label="Создана">
            {createdAt.toLocaleDateString()} {createdAt.toLocaleTimeString()}
          </Descriptions.Item>
          <Descriptions.Item label="Обновлена">
            {updatedAt.toLocaleDateString()} {updatedAt.toLocaleTimeString()}
          </Descriptions.Item>
          <Descriptions.Item label="Срок">
            {dueDate
              ? `${dueDate.toLocaleDateString()} ${dueDate.toLocaleTimeString()}`
              : "Не задан"}
          </Descriptions.Item>
          <Descriptions.Item label="Внешний ID">
            {request.externalReferenceId || <Text type="secondary">Не задан</Text>}
          </Descriptions.Item>
          <Descriptions.Item label="Связанный объект — тип">
            {request.relatedEntityType || <Text type="secondary">Не задан</Text>}
          </Descriptions.Item>
          <Descriptions.Item label="Связанный объект — ID">
            {request.relatedEntityId || <Text type="secondary">Не задан</Text>}
          </Descriptions.Item>
          <Descriptions.Item label="Описание" span={2}>
            {request.description || <Text type="secondary">Нет описания</Text>}
          </Descriptions.Item>
        </Descriptions>
      </div>

      <Tabs
        defaultActiveKey="history"
        items={[
          {
            key: "history",
            label: "История",
            children: (
              <>
                {historyError && (
                  <Alert
                    type="error"
                    message="Ошибка при загрузке истории заявки"
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
            label: "Комментарии",
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