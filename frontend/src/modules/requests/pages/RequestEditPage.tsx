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
import { t } from "../../../core/i18n/t";

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
            : t("requests.edit.error.loadFormData");
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
        error instanceof Error ? error.message : t("requests.edit.error.save");
      setState({ kind: "error", message });
    } finally {
      setSubmitting(false);
    }
  };

  if (state.kind === "loading") {
    return (
      <div
        data-testid="request-edit-loading"
        style={{
          minHeight: "40vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Spin
          tip={
            isEdit ? t("requests.edit.loading.edit") : t("requests.edit.loading.create")
          }
        />
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div>
        <Alert
          data-testid="request-edit-error-alert"
          type="error"
          message={
            isEdit
              ? t("requests.edit.error.load.title")
              : t("requests.edit.error.prepare.title")
          }
          description={state.message}
          showIcon
          style={{ marginBottom: 16 }}
        />
        <Button data-testid="request-edit-back-button" onClick={handleCancel}>
          {t("common.actions.back")}
        </Button>
      </div>
    );
  }

  if (isEdit && !request) {
    return (
      <Alert
        data-testid="request-edit-not-found-alert"
        type="error"
        message={t("requests.edit.error.notFound.title")}
        description={t("requests.edit.error.notFound.description")}
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
    <Card data-testid="request-edit-card">
      <Title level={3} style={{ marginBottom: 16 }}>
        {isEdit ? t("requests.edit.title.edit") : t("requests.edit.title.create")}
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
