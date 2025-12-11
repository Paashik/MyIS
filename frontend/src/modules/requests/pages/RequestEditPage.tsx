import React, { useEffect, useState } from "react";
import { Alert, Button, Card, Spin, Typography } from "antd";
import { useNavigate, useParams } from "react-router-dom";
import {
  CreateRequestPayload,
  RequestDto,
  RequestTypeDto,
  UpdateRequestPayload,
} from "../api/types";
import {
  createRequest,
  getRequest,
  getRequestTypes,
  updateRequest,
} from "../api/requestsApi";
import { RequestForm, RequestFormValues } from "../components/RequestForm";

const { Title } = Typography;

type PageState =
  | { kind: "loading" }
  | { kind: "loaded" }
  | { kind: "error"; message: string };

export const RequestEditPage: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();

  const [state, setState] = useState<PageState>({ kind: "loading" });
  const [requestTypes, setRequestTypes] = useState<RequestTypeDto[]>([]);
  const [request, setRequest] = useState<RequestDto | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setState({ kind: "loading" });

      try {
        const typesPromise = getRequestTypes();
        const requestPromise = isEdit && id ? getRequest(id) : Promise.resolve(null);

        const [types, existing] = await Promise.all([typesPromise, requestPromise]);

        if (cancelled) return;

        setRequestTypes(types);
        if (existing) {
          setRequest(existing as RequestDto);
        }

        setState({ kind: "loaded" });
      } catch (error) {
        if (cancelled) return;

        const message =
          error instanceof Error
            ? error.message
            : "Не удалось загрузить данные для формы заявки";
        setState({ kind: "error", message });
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [id, isEdit]);

  const handleCancel = () => {
    if (isEdit && id) {
      navigate(`/requests/${encodeURIComponent(id)}`);
    } else {
      navigate("/requests");
    }
  };

  const handleSubmit = async (values: RequestFormValues) => {
    setSubmitting(true);

    try {
      if (isEdit && id) {
        const payload: UpdateRequestPayload = {
          title: values.title,
          description: values.description,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          externalReferenceId: values.externalReferenceId,
        };

        const updated = await updateRequest(id, payload);
        navigate(`/requests/${encodeURIComponent(updated.id)}`, { replace: true });
      } else {
        const payload: CreateRequestPayload = {
          requestTypeId: values.requestTypeId,
          title: values.title,
          description: values.description,
          dueDate: values.dueDate,
          relatedEntityType: values.relatedEntityType,
          relatedEntityId: values.relatedEntityId,
          externalReferenceId: values.externalReferenceId,
        };

        const created = await createRequest(payload);
        navigate(`/requests/${encodeURIComponent(created.id)}`, { replace: true });
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Не удалось сохранить заявку";
      setState({ kind: "error", message });
    } finally {
      setSubmitting(false);
    }
  };

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
        <Spin tip={isEdit ? "Загрузка заявки..." : "Подготовка формы..."} />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          type="error"
          message={isEdit ? "Ошибка загрузки заявки" : "Ошибка подготовки формы"}
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button onClick={handleCancel}>Назад</Button>
      </div>
    );
  }

  if (isEdit && !request) {
    return (
      <Alert
        type="error"
        message="Заявка не найдена"
        description="Невозможно отредактировать несуществующую заявку."
        showIcon
      />
    );
  }

  const initialValues =
    isEdit && request
      ? {
          requestTypeId: request.requestTypeId,
          title: request.title,
          description: request.description ?? "",
          dueDate: request.dueDate ?? undefined,
          relatedEntityType: request.relatedEntityType ?? "",
          relatedEntityId: request.relatedEntityId ?? "",
          externalReferenceId: request.externalReferenceId ?? "",
        }
      : undefined;

  return (
    <Card>
      <Title level={3} style={{ marginBottom: 16 }}>
        {isEdit ? "Редактирование заявки" : "Создание заявки"}
      </Title>

      <RequestForm
        mode={isEdit ? "edit" : "create"}
        initialValues={initialValues}
        requestTypes={requestTypes}
        submitting={submitting}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
      />
    </Card>
  );
};